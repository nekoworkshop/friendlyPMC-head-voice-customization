using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class pitAIBossPlayer : AIBossPlayer
    {
        private AIBossPlayerLogic aBossLogic;

        public BotsGroup bossGroup = null;

        private List<BotOwner> bossEnemies = new List<BotOwner>();
        public pitAIBossPlayer(Player player) : base(player)
        {
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

        public void prioritizeEnemy(BotOwner follower)
        {

            // make the closest enemy of boss, the enemy
            if (bossEnemies.Count > 0)
            {
                BotOwner newEnemy = null;
                float dist = Mathf.Infinity;
                foreach (var item in bossEnemies)
                {
                    if ((this.Position - item.Position).sqrMagnitude < dist)
                    {
                        newEnemy = item;
                    }
                }
                if (newEnemy != null)
                {
                    BotSettingsClass botSettingsClass = new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(newEnemy.ProfileId), bossGroup, EBotEnemyCause.initCauseEnemy);

                    follower.Memory.AddEnemy(newEnemy, botSettingsClass, false);
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            aBossLogic.Dispose();
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

            //botsController.GetClosestZone
        }

        public void OnHit(EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            if (damage > 0f && damageInfo.Player != null && damageInfo.Player.IsAI && !BossPlayer.Instance.IsFollower(damageInfo.Player.AIData.BotOwner))
            {
                _lastTimeHit = Time.time;
                try
                {
                    _aiplayer.bossGroup.CheckAndAddEnemy(damageInfo.Player.AIData.BotOwner);

                }
                catch (Exception)
                {
                    Components.Logger.LogInfo("Can't add enemy to group");
                }

                _aiplayer.AddEnemy(damageInfo.Player.AIData.BotOwner);
            }
        }

        public override void Activate()
        {

        }

        public override void BossLogicUpdate()
        {
            Logger.LogInfo("prioritize enemies");
            _aiplayer.Followers.ForEach(follower =>
            {
                _aiplayer.prioritizeEnemy(follower);
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
    internal class BossPlayer
    {
        public static BossPlayer Instance;

        public BossPlayer()
        {
            Instance = this;
            _bosses = new Dictionary<string, pitAIBossPlayer>();
            _followers = new List<BotFollowerPlayer> { };

            Logger.LogInfo("BossPlayer Instanced");
        }

        private Dictionary<string, pitAIBossPlayer> _bosses { get; set; }
        private List<BotFollowerPlayer> _followers { get; set; }

        private List<CustomNavigationPoint> navigationPoints;

        public pitAIBossPlayer AddBossPlayer(Player player)
        {
            WildSpawnType roleType = player.Profile.Info.Settings.Role;
            player.Profile.Info.Settings.Role = WildSpawnType.bossKnight; // temp switch to boss role
            pitAIBossPlayer playerBoss = new pitAIBossPlayer(player);
            player.Profile.Info.Settings.Role = roleType; // revert role back to original

            if(!playerBoss.IAmBoos)
            {
                Logger.LogInfo($"Could not make player {player.Profile.Nickname} as BOSS!");
                return null;
            } else
            {

                Logger.LogInfo($"Made player {player.Profile.Nickname} a BOSS");
            }

            string name = player.ProfileId;

            _bosses[name] = playerBoss;

            // get info about ALL available covers
            if (navigationPoints == null)
            {
                AICoversData[] aICoversData = UnityEngine.Object.FindObjectsOfType<AICoversData>();

                List<CustomNavigationPoint> customNavigationPoints = new List<CustomNavigationPoint>();

                if (aICoversData != null)
                {

                    foreach (AICoversData cover in aICoversData)
                    {
                        int id = player.Id;
                        for (int i = 0; i < cover.MaxX; i++)
                        {
                            for (int j = 0; j < cover.MaxY; j++)
                            {
                                for (int k = 0; k < cover.MaxZ; k++)
                                {

                                    NavGraphVoxelSimple navGraphVoxelSimple = cover.VoxelesArray[i, j, k];
                                    if (navGraphVoxelSimple != null && navGraphVoxelSimple.Points != null)
                                    {
                                        foreach (GroupPoint groupPoint in navGraphVoxelSimple.Points)
                                        {
                                            customNavigationPoints.Add(groupPoint.CreateCustomNavigationPoint(id));
                                        }
                                    }
                                }
                            }
                        }
                    }

                }

                navigationPoints = customNavigationPoints;
            }

            return playerBoss;
        }

        public void RemoveBossPlayer(string name)
        {
            if (_bosses.ContainsKey(name))
            {
                pitAIBossPlayer boss = _bosses[name];
                boss.Followers.ForEach(fl =>
                {
                    _followers.ForEach(follower =>
                    {
                        if (follower.IsBot(fl))
                        {
                            _followers.Remove(follower);
                            boss.RemoveFollower(fl);
                        }
                    });
                });



                boss.Dispose();
                _bosses.Remove(name);
            }
        }

        public BotFollowerPlayer AddFollower(BotOwner bot, pitAIBossPlayer player)
        {
            BotFollowerPlayer _follower = null;

            _followers.ForEach(follower =>
            {
                if (follower.IsBot(bot))
                {
                    _follower = follower;
                }
            });

            if (_follower != null)
            {
                _followers.Remove(_follower);
            }

            _follower = new BotFollowerPlayer(bot, player);

            _followers.Add(_follower);

            return _follower;
        }

        public void RemoveFollower(BotOwner bot, pitAIBossPlayer player)
        {

            BotFollowerPlayer _follower = null;

            _followers.ForEach(follower =>
            {
                if (follower.IsBot(bot))
                {
                    _follower = follower;
                }
            });

            if (_follower != null)
            {
                _followers.Remove(_follower);
            }

            player.RemoveFollower(bot);
        }

        public bool IsFollower(BotOwner bot, AIBossPlayer boss = null)
        {

            if(boss != null)
            {
                return bot.BotFollower.BossToFollow != null && bot.BotFollower.BossToFollow == boss;
            }

            BotFollowerPlayer _follower = null;

            foreach (var item in _followers)
            {
                if (item.IsBot(bot))
                {
                    _follower = item;
                    break;
                }
            }

            return _follower != null;
        }

        public pitAIBossPlayer GetBossPlayer(string name)
        {
            Logger.LogInfo($"Boss name {name}");
            foreach (var item in _bosses.Keys)
            {
                Logger.LogInfo($"_bosses key {item}");
            }
            if (!_bosses.ContainsKey(name))
            {
                return null;
            }
            return _bosses[name];
        }

        public List<BotFollowerPlayer> GetBossFollowers(string name)
        {
            List<BotFollowerPlayer> botFollowers = new List<BotFollowerPlayer>();

            if (!_bosses.ContainsKey(name) || _bosses[name] == null)
            {
                return botFollowers;
            }

            pitAIBossPlayer player = _bosses[name];

            foreach (var item in _followers)
            {
                if (item.GetBoss() == player)
                {
                    botFollowers.Add(item);
                }
            }

            return botFollowers;
        }

        public List<CustomNavigationPoint> GetCovers()
        {
            return navigationPoints;
        }

        public void SpawnFollowers()
        {

        }
    }
}
