using Comfort.Common;
using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace friendlyPMC.Components
{
    internal class pitAIBossPlayer : AIBossPlayer
    {
        private AIBossPlayerLogic aBossLogic;

        public BotsGroup bossGroup = null;

        public readonly Player realPlayer;

        private List<BotOwner> bossEnemies = new List<BotOwner>();
        public pitAIBossPlayer(Player player) : base(player)
        {
            realPlayer = player;
            aBossLogic = new AIBossPlayerLogic(player, this);
        }

        public new AIBossPlayerLogic GetBossLogic()
        {
            return aBossLogic;
        }

        public void AddEnemy(BotOwner bot)
        {
            if (!bossEnemies.Contains(bot))
            {
                bossEnemies.Add(bot);

                bot.HealthController.DiedEvent += (EDamageType type) =>
                {
                    RemoveEnemy(bot);
                };
                bot.LeaveData.OnLeave += (BotOwner _bot) =>
                {
                    RemoveEnemy(bot);
                };
            }

        }
        public void RemoveEnemy(BotOwner bot)
        {
            if (bossEnemies.Contains(bot))
            {
                bossEnemies.Remove(bot);
            }
        }

        public List<BotOwner> GetEnemies()
        {
            return bossEnemies;
        }

        public void PrioritizeEnemy(BotOwner follower, BotOwner enemy)
        {

            // make the closest enemy of boss, the enemy
            if(enemy != null)
            {
                BotSettingsClass botSettingsClass = new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(enemy.ProfileId), bossGroup, EBotEnemyCause.checkAddTODO);

                follower.Memory.AddEnemy(enemy, botSettingsClass, false);
                EnemyInfo info;
                follower.EnemiesController.EnemyInfos.TryGetValue(enemy.GetPlayer, out info);
                if (info != null)
                {
                    info.PriorityIndex = 0;
                }
            }
        }

        public BotOwner ClosestEnemy()
        {
            BotOwner enemy = null;

            if (bossEnemies.Count > 0)
            {
                float dist = Mathf.Infinity;
                
                foreach (var item in bossEnemies)
                {
                    float range = (this.Position - item.Position).sqrMagnitude;
                    if (range < dist)
                    {
                        enemy = item;
                        dist = range;
                    }
                }
            }

            return enemy;
        }

        public void DisposeBoss()
        {
            if(bossGroup != null)
            {
                bossGroup.RemoveInfo(this.Player());
            }
            aBossLogic.Dispose();

            Logger.LogInfo("Player Boss Disposed");
        }
    }
    internal class AIBossPlayerLogic : GClass363
    {
        private Player _player;
        private pitAIBossPlayer _aiplayer;

        private string tactic;
        public AIBossPlayerLogic(Player player, pitAIBossPlayer aiplayer) : base(null, null)
        {
            player.HealthController.ApplyDamageEvent += OnHit;
            _player = player;
            _aiplayer = aiplayer;
        }

        public void OnHit(EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            if (damage > 0f && damageInfo.Player != null && damageInfo.Player.IsAI && !BossPlayers.Instance.IsFollower(damageInfo.Player.AIData.BotOwner))
            {
                _lastTimeHit = Time.time;
                try
                {
                    if (_aiplayer.bossGroup != null)
                    {
                        _aiplayer.bossGroup.CheckAndAddEnemy(damageInfo.Player.AIData.BotOwner);
                        _aiplayer.AddEnemy(damageInfo.Player.AIData.BotOwner);
                    }

                }
                catch (Exception)
                {
                    Components.Logger.LogInfo("Can't add enemy to group");
                }
            }
        }

        public override void Activate()
        {

        }

        public override void BossLogicUpdate()
        {

        }

        public override void Dispose()
        {
            _player.HealthController.ApplyDamageEvent -= OnHit;
        }

        public override void SetPatrolMode()
        {

        }

        public void SetTactic(string prefTactic)
        {
            if (prefTactic == null) tactic = null;

            else if (prefTactic == "push") tactic = "push";
            else tactic = "defend";
        }

        public string GetTactic()
        {
            return tactic;
        }
    }
}
