using EFT;
using friendlyPMC.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Modules
{
    /**
     *  Helper class to manage Boss Players and their followers
     */
    internal class BossPlayers
    {
        public static BossPlayers Instance { get; private set; }

        private Dictionary<string, pitAIBossPlayer> _bosses;
        private List<BotFollowerPlayer> _followers;
        private List<string> _shallBeFollower;
        private List<int> _botsGroup;

        private List<CustomNavigationPoint> _groupPoints;

        private List<string> _removedBosses;

        public bool IsDisposed = false;

        private static readonly List<string> _excludedColliderNames = new List<string>
        {
            "metall_fence_2",
            "metallstolb",
            "stolb",
            "fonar_stolb",
            "fence_grid",
            "metall_fence_new",
            "ladder_platform",
            "frame_L",
            "frame_small_collider",
            "bump2x_p3_set4x",
            "bytovka_ladder",
            "sign",
            "sign17_lod",
            "ograda1",
            "ladder_metal"
        };

        public BossPlayers()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            _bosses = new Dictionary<string, pitAIBossPlayer>();
            _followers = new List<BotFollowerPlayer> { };
            _shallBeFollower = new List<string> { };
            _removedBosses = new List<string> { };
            _botsGroup = new List<int> { };

            _groupPoints = new List<CustomNavigationPoint>();
        }

        public static void Dispose()
        {
            if (Instance != null)
            {
                Instance.Destroy();
                Instance = null;
            }
        }
       

        private pitAIBossPlayer AddBossPlayer(Player player)
        {
            if(_bosses.ContainsKey(player.ProfileId)) return _bosses[player.ProfileId];


            WildSpawnType roleType = player.Profile.Info.Settings.Role;
            player.Profile.Info.Settings.Role = WildSpawnType.bossKnight; // temp switch to boss role
            pitAIBossPlayer playerBoss = new pitAIBossPlayer(player);
            player.Profile.Info.Settings.Role = roleType; // revert role back to original

            if (!playerBoss.IAmBoos)
            {
                Logger.LogInfo($"Could not make player {player.Profile.Nickname} as BOSS");
                return null;
            }
            else
            {
                Logger.LogInfo($"Made player {player.Profile.Nickname} a BOSS");
            }

            string name = player.ProfileId;

            if(string.IsNullOrEmpty(player.Profile.Info.GroupId))
            {
                player.Profile.Info.GroupId = "bossGroup_" + name;
            }

            if (_removedBosses.Contains(name))
            {
                _removedBosses.Remove(name);
            }

            _bosses[name] = playerBoss;

            if (_groupPoints.Count > 0) return playerBoss;



            AICoversData[] aICoversData = UnityEngine.Object.FindObjectsOfType<AICoversData>();

            if (aICoversData != null)
            {

                foreach (AICoversData cover in aICoversData)
                {
                    int id = 0;
                    for (int i = 0; i < cover.MaxX; i++)
                    {
                        id += i;
                        for (int j = 0; j < cover.MaxY; j++)
                        {
                            id += j;
                            for (int k = 0; k < cover.MaxZ; k++)
                            {
                                id += k;

                                NavGraphVoxelSimple navGraphVoxelSimple = cover.VoxelesArray[i, j, k];
                                if (navGraphVoxelSimple != null && navGraphVoxelSimple.Points != null)
                                {
                                    foreach (GroupPoint groupPoint in navGraphVoxelSimple.Points)
                                    {
                                        if (groupPoint.CoverLevel == CoverLevel.Stay || groupPoint.CoverLevel == CoverLevel.Sit)
                                        {
                                            Collider[] colliders = new Collider[10];
                                            int numColliders = Physics.OverlapSphereNonAlloc(groupPoint.Position, 1.5f, colliders);

                                            bool isgood = true;
                                            for (int x = 0; i < numColliders; i++)
                                            {
                                                Collider collider = colliders[x];

                                                if (_excludedColliderNames.Contains(collider.transform?.parent?.name))
                                                {
                                                    isgood = false;
                                                    break;
                                                }
                                            }

                                            if (isgood)
                                            {
                                                try
                                                {
                                                    _groupPoints.Add(groupPoint.CreateCustomNavigationPoint(id));
                                                    id += 1;
                                                } catch
                                                {

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            return playerBoss;
        
        }

        private bool RemoveBossPlayer(string name)
        {
            if (_bosses.ContainsKey(name))
            {
                pitAIBossPlayer boss = _bosses[name];

                List<BotOwner> ownersToRemove = new List<BotOwner>();
                List<BotFollowerPlayer> followersToRemove = new List<BotFollowerPlayer>();

                boss.Followers.ForEach(fl =>
                {
                    ownersToRemove.Add(fl);
                    _followers.ForEach(follower =>
                    {
                        if (follower.IsBot(fl))
                        {
                            followersToRemove.Add(follower);
                        }
                    });
                });

                ownersToRemove.ForEach(follower =>
                {
                    boss.RemoveFollower(follower);
                });

                followersToRemove.ForEach(_follower =>
                {
                    _followers.Remove(_follower);
                    _follower.Dismiss();
                });
                
                boss.Followers.Clear();

                boss.DisposeBoss();

                _bosses.Remove(name);
                _removedBosses.Add(name);
                return true;
            }
            else if (_removedBosses.Contains(name))
            {
                return true;
            }

            return false;
        }

        private void Destroy()
        {
            if (IsDisposed) return;

            if (_bosses.Count > 0)
            {
                List<string> keys = new List<string>(_bosses.Keys);
                foreach (string key in keys)
                {
                    RemoveBossPlayer(key);
                }
            }
            _bosses.Clear();
            _removedBosses.Clear();
            _followers.Clear();
            _shallBeFollower.Clear();

            IsDisposed = true;
            Instance = null;
        }

        private BotFollowerPlayer AddBotFollower(BotOwner bot, pitAIBossPlayer player, bool squadMate = false, WildSpawnType role = WildSpawnType.assault, string tactic = "Default")
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
                return _follower;
            }

            if(_shallBeFollower.Contains(bot.name)) _shallBeFollower.Remove(bot.name);

            bool isAIBoss = false;

            foreach (var item in Utils.Props.BossFollowersType)
            {
                if(role == item)
                {
                    isAIBoss = true;
                    break;
                }
            }

            if (!isAIBoss)
            {
                _follower = new BotFollowerPlayer(bot, player, bot.Side != EPlayerSide.Savage && squadMate);
            }
            else
            {
                _follower = new BossFollowerPlayer(bot, player, role);
                
            }
            try
            {
                _follower.Init();

            }
            catch( Exception e)
            {
                Modules.Logger.LogError("Failed to init follower");
                Modules.Logger.LogError(e);
                return null;
            }

            if (!isAIBoss)
            {
                // scavs and picked up followers have special tactic
                if (bot.Side == EPlayerSide.Savage || !squadMate)
                {
                    (bot.Brain.BaseBrain as FollowerBrain).SetBossTactic("Assist");
                }
                else
                {
                    (bot.Brain.BaseBrain as FollowerBrain).SetBossTactic(tactic);
                }
            }

            _followers.Add(_follower);

            return _follower;
        }

        private bool IsBotFollower(BotOwner bot, AIBossPlayer boss = null)
        {
            if (bot == null || bot.BotFollower == null || !bot.BotFollower.HaveBoss) return false;

            BotFollowerPlayer _follower = null;

            foreach (var item in _followers)
            {
                if (item != null && item.IsBot(bot))
                {
                    _follower = item;
                    break;
                }
            }

            if (_follower != null && boss != null && bot != null && bot.BotFollower.HaveBoss)
            {
                return bot.BotFollower.BossToFollow.Player().ProfileId == boss.Player().ProfileId;
            }

            return _follower != null;
        }

        private void RemoveBotFollower(BotOwner bot, pitAIBossPlayer player)
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
                if (player.bossGroup != null)
                {
                    player.bossGroup.RemoveAlly(bot);

                }

                player.RemoveFollower(bot);
                bot.BotFollower.BossToFollow = null;
            }
        }

        public BotFollowerPlayer GetFollower(BotOwner bot)
        {
            if (bot == null) return null;
            
            BotFollowerPlayer _follower = null;

            foreach (var item in _followers)
            {
                if (item != null && item.IsBot(bot))
                {
                    _follower = item;
                    break;
                }
            }
            return _follower;

        }

        private bool IsBoss(string id)
        {
            if(_bosses == null) return false;

            return _bosses.ContainsKey(id);
        }

        public pitAIBossPlayer GetBossPlayer(string name)
        {
            if (_bosses == null) return null;

            if (!_bosses.ContainsKey(name))
            {
                return null;
            }
            return _bosses[name];
        }

        public Dictionary<string, pitAIBossPlayer> GetBossPlayers()
        {
            return _bosses;
        }

        private List<BotFollowerPlayer> GetBossFollowers(string name)
        {
            List<BotFollowerPlayer> botFollowers = new List<BotFollowerPlayer>();

            if (_bosses == null) return botFollowers;

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

        private List<CustomNavigationPoint> GetCovers()
        {
            return _groupPoints;
        }

        public static pitAIBossPlayer GetBoss(string name)
        {
            if(Instance  == null) return null;  
            return Instance.GetBossPlayer(name);
        }

        public static bool IsPlayerBoss(string profileId)
        {
            if (Instance == null) return false;

            return Instance.IsBoss(profileId);
        }

        public static List<BotFollowerPlayer> GetFollowersByBoss(string bossName)
        {
            if (Instance == null) return new List<BotFollowerPlayer>();
            return Instance.GetBossFollowers(bossName);
        }

        public static List<BotFollowerPlayer> GetFollowers()
        {
            if (Instance == null) return new List<BotFollowerPlayer>();
            return Instance._followers;
        }

        public static bool IsFollower(BotOwner bot, AIBossPlayer boss = null)
        {
            if (Instance == null || bot == null) return false;
            return Instance.IsBotFollower(bot, boss) || Instance._shallBeFollower.Contains(bot.name);
        }

        public static bool WillBeFollower(BotOwner bot)
        {
            if (Instance == null || bot == null) return false;
            return Instance._shallBeFollower.Contains(bot.name);
        }
        public static List<CustomNavigationPoint> GetAICovers()
        {
            return Instance.GetCovers();
        }

        public static void AddGroupToBoss(pitAIBossPlayer player, BotsGroup group)
        {
            player.bossGroup = group;
            player.bossGroup.Lock();
            // prevent group from ever making the player an enemy
            player.bossGroup.OnEnemyAdd += (IPlayer pl, EBotEnemyCause cause) =>
            {
                if (pl != null)
                {
                    if (player.Player().ProfileId == pl.ProfileId)
                    {
                        player.bossGroup.RemoveEnemy(player.Player());
                        player.bossGroup.AddAlly(player.realPlayer);
                    }
                    else if (pl.IsAI && player.bossGroup.Contains(pl.AIData.BotOwner))
                    {
                        player.bossGroup.RemoveEnemy(pl);
                        player.bossGroup.AddAlly(pl.AIData.Player);
                    }
                }
            };
            if (!Instance._botsGroup.Contains(group.Id)) Instance._botsGroup.Add(group.Id);

            player.bossGroup.AddAlly((Player)player.Player());

            foreach (var enemy in player.GetEnemies())
            {
                player.bossGroup.AddEnemy(enemy, EBotEnemyCause.addPlayerToBoss);
            }
        }

        public static bool IsBossGroup(int id)
        {
            if (Instance == null) return false;
            return Instance._botsGroup.Contains(id);
        }

        public static pitAIBossPlayer GetBossByGroup(int id)
        {
            if(Instance == null) return null;
            if(Instance._bosses.Count == 0) return null;
            if(Instance._botsGroup.Contains(id))
            {
                foreach (var item in Instance._bosses)
                {
                    if (item.Value.bossGroup != null && item.Value.bossGroup.Id == id)
                    {
                        return item.Value;
                    }
                }
            }

            return null;
        }

        public static void RemoveFollower(BotOwner bot, pitAIBossPlayer player)
        {
            if (Instance == null) return;
            Instance.RemoveBotFollower(bot, player);
        }

        public static pitAIBossPlayer AddPlayerAsBoss(Player player)
        {
            return Instance.AddBossPlayer(player);
        }

        public static void RemovePlayerBoss(string profileId)
        {
            Instance.RemoveBossPlayer(profileId);
        }

        public static BotFollowerPlayer AddFollower(BotOwner bot, pitAIBossPlayer player, bool squadMate = false, WildSpawnType role = WildSpawnType.assault, string tactic = "Default")
        {
            return Instance.AddBotFollower(bot,player,squadMate,role,tactic);
        }

        public static void ShallBeFollower(BotOwner bot)
        {
            if(!Instance._shallBeFollower.Contains(bot.name)) Instance._shallBeFollower.Add(bot.name);
        }
    }
}
