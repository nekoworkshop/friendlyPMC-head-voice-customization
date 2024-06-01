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

        public void PrioritizeEnemy(BotOwner follower)
        {

            // make the closest enemy of boss, the enemy
            if (bossEnemies.Count > 0)
            {
                BotOwner newEnemy = null;
                float dist = Mathf.Infinity;
                foreach (var item in bossEnemies)
                {
                    if ((this.Position - item.Position).magnitude < dist)
                    {
                        newEnemy = item;
                    }
                }
                if (newEnemy != null)
                {
                    BotSettingsClass botSettingsClass = new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(newEnemy.ProfileId), bossGroup, EBotEnemyCause.checkAddTODO);

                    follower.Memory.AddEnemy(newEnemy, botSettingsClass, false);
                    EnemyInfo info;
                    follower.EnemiesController.EnemyInfos.TryGetValue(newEnemy.GetPlayer, out info);
                    if(info != null)
                    {
                        info.PriorityIndex = 0;
                    }
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
                    if ((this.Position - item.Position).magnitude < dist)
                    {
                        enemy = item;
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
            _aiplayer.Followers.ForEach(follower =>
            {
                _aiplayer.PrioritizeEnemy(follower);
            });
        }

        public override void Dispose()
        {
            _player.HealthController.ApplyDamageEvent -= OnHit;
        }

        public override void SetPatrolMode()
        {

        }
    }
}
