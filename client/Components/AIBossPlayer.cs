using Comfort.Common;
using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace friendlyPMC.Components
{
    internal class pitAIBossPlayer : AIBossPlayer
    {
        private AIBossPlayerLogic aBossLogic;

        public BotsGroup bossGroup = null;

        public readonly Player realPlayer;

        private List<BotOwner> bossEnemies = new List<BotOwner>();

        private List<CustomNavigationPoint> coverPoints;

        private Coroutine coverCoroutine;

        private float maximumDistance = 100f;
        public pitAIBossPlayer(Player player) : base(player)
        {
            realPlayer = player;

            aBossLogic = new AIBossPlayerLogic(player, this);
            coverPoints = new List<CustomNavigationPoint>();
            
            player.HealthController.DiedEvent += OnDead;

            maximumDistance = friendlyPMC.maximumRadius.Value;

            SetAreaCovers();
            coverCoroutine = player.StartCoroutine(UpdateCoversCoroutine());
        }

        private void OnDead(EDamageType _damageType)
        {
            if (Followers != null && Followers.Count > 0)
            {
                Followers.ForEach(follower =>
                {
                    if (follower != null && GClass760.Random(1, 100) > friendlyPMC.returnChanceDeath.Value)
                    {

                        var flw = BossPlayers.Instance.GetFollower(follower);

                        if (flw != null && flw.IsSquadMate)
                            InteractableObjects.ClearStoredItems(follower.ProfileId);
                    }
                });
            }
            BossPlayers.Instance.RemoveBossPlayer(realPlayer.ProfileId);
        }

        public new AIBossPlayerLogic GetBossLogic()
        {
            return aBossLogic;
        }

        public void SetAreaCovers()
        {
            Task.Run(() =>
            {

                List<CustomNavigationPoint> covers = new List<CustomNavigationPoint>();
                float radius = maximumDistance;
                float lastDist = 0f;
                Vector3 centerPos = realPlayer.Transform.position;

                BossPlayers.Instance.GetCovers().ForEach(point =>
                {
                    float sqrDist = (centerPos - point.Position).sqrMagnitude;

                    if (sqrDist <= lastDist)
                    {
                        covers.Add(point);

                    }
                    else if (Vector3.Distance(centerPos, point.Position) <= radius)
                    {
                        lastDist = sqrDist;
                        covers.Add(point);
                    }
                });

                coverPoints = covers;
            });
        }

        private IEnumerator UpdateCoversCoroutine()
        {
            while (true)
            {
                SetAreaCovers();
                yield return new WaitForSeconds(2f);
            }
        }
        public List<CustomNavigationPoint> GetAreaCovers()
        {
            return coverPoints;
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
            realPlayer.HealthController.DiedEvent -= OnDead;

            if (bossGroup != null)
            {
                bossGroup.RemoveInfo(Player());
            }
            aBossLogic.Dispose();

            // Stop the coroutine when the boss is disposed
            if (coverCoroutine != null)
            {
                realPlayer.StopCoroutine(coverCoroutine);
            }

            Logger.LogInfo("Player Boss Disposed");
        }

        public new void Dispose()
        {
            // do nothing
            Logger.LogInfo("pitAIBossPlayer Dispose called");
        }

        public new void OfferBot(BotOwner bot)
        {
            // do nothing, this is called by the game and we don't want followers to be added automatically
        }

        public void AddFollower(BotOwner bot)
        {
            Followers.Add(bot);
            bot.BotFollower.PatrolDataFollower.InitPlayer(realPlayer);
            bot.BotFollower.Index = Followers.Count - 1;
            bot.BotFollower.BossToFollow = this;
            
            bot.BotFollower.PatrolDataFollower.Activate();
            bot.BotFollower.PatrolDataFollower.SetIndex(bot.BotFollower.Index);

            PatrolMode mode = PatrolMode.follower;
            PatrolMode mode2 = PatrolMode.simple;

            PatrolPointChooserBasic pointChooser = PatrollingData.GetPointChooser(bot, mode2, bot.SpawnProfileData);
            bot.PatrollingData.SetMode(mode, pointChooser);
            bot.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Protect, false, -1f);
            bot.BotFollower.BossFindAction();
        }
    }
    internal class AIBossPlayerLogic : GClass363
    {
        private Player _player;
        private pitAIBossPlayer _aiplayer;
        public AIBossPlayerLogic(Player player, pitAIBossPlayer aiplayer) : base(null, null)
        {
            player.BeingHitAction += OnHit;
            _player = player;
            _aiplayer = aiplayer;
        }

        public void OnHit(DamageInfo arg1, EBodyPart arg2, float arg3)
        {
            if (
                arg1.Player != null && arg1.Player.IsAI && 
                arg1.Player.AIData != null && 
                arg1.Player.AIData.BotOwner != null && 
                !BossPlayers.Instance.IsFollower(arg1.Player.AIData.BotOwner)
                )
            {
                _lastTimeHit = Time.time;
                try
                {
                    if (_aiplayer.bossGroup != null)
                    {
                        _aiplayer.bossGroup.CheckAndAddEnemy(arg1.Player.iPlayer);
                        _aiplayer.bossGroup.ReportAboutEnemy(arg1.Player.iPlayer, EEnemyPartVisibleType.visible);
                        _aiplayer.AddEnemy(arg1.Player.AIData.BotOwner);
                    }

                }
                catch (Exception e)
                {
                    Logger.LogInfo("Failed to add Enemy to group: "+e.Message);
                }
            }
        }
        

        public override void Activate()
        {
            if (_aiplayer.Followers.Count > 0)
            {
                foreach (var item in _aiplayer.Followers)
                {
                    if(item.IsRole(WildSpawnType.bossKnight))
                    {
                        item.Boss.BossLogic.Activate();
                        break;
                    }
                }
            }
        }

        public override void BossLogicUpdate()
        {
            if (_aiplayer.Followers.Count > 0)
            {
                foreach (var item in _aiplayer.Followers)
                {
                    if (item.IsRole(WildSpawnType.bossKnight))
                    {
                        item.Boss.BossLogic.BossLogicUpdate();
                        break;
                    }
                }
            }
        }

        public override void Dispose()
        {
            _player.BeingHitAction -= OnHit;
        }

        public override void SetPatrolMode()
        {

        }

    }
}
