using EFT;
using friendlyPMC.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace friendlyPMC.Modules
{
    internal class BossPlayers
    {
        public static BossPlayers Instance { get; private set; }

        private Dictionary<string, pitAIBossPlayer> _bosses { get; set; }
        private List<BotFollowerPlayer> _followers { get; set; }
        private List<int> _botsGroup { get; set; }

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
       

        public pitAIBossPlayer AddBossPlayer(Player player)
        {
            if(_bosses.ContainsKey(player.ProfileId)) return _bosses[player.ProfileId];

            WildSpawnType roleType = player.Profile.Info.Settings.Role;
            player.Profile.Info.Settings.Role = WildSpawnType.bossKnight; // temp switch to boss role
            pitAIBossPlayer playerBoss = new pitAIBossPlayer(player);
            player.Profile.Info.Settings.Role = roleType; // revert role back to original

            if (!playerBoss.IAmBoos)
            {
                Components.Logger.LogInfo($"Could not make player {player.Profile.Nickname} as BOSS");
                return null;
            }
            else
            {
                Components.Logger.LogInfo($"Made player {player.Profile.Nickname} a BOSS");
            }

            string name = player.ProfileId;

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

        public bool RemoveBossPlayer(string name)
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

        public void Destroy()
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
            _bosses = null;
            _removedBosses = null;
            _followers = null;

            IsDisposed = true;
            Instance = null;
        }

        public BotFollowerPlayer AddFollower(BotOwner bot, pitAIBossPlayer player, bool squadMate = false, WildSpawnType role = WildSpawnType.assault)
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


            bool isAIBoss = false;

            foreach (var item in Utils.Utils.BossFollowersRoles)
            {
                if(role == item)
                {
                    isAIBoss = true;
                    break;
                }
            }

            if (!isAIBoss)
                _follower = new BotFollowerPlayer(bot, player,squadMate);
            else
                _follower = new BossFollowerPlayer(bot, player, role);

            _followers.Add(_follower);

            return _follower;
        }

        public void RemoveBotFollower(BotOwner bot, pitAIBossPlayer player,bool dismissed = false)
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
                    player.bossGroup.RemoveAlly(bot);
                
                // reset the bot receiever
                if (dismissed && bot.HealthController.IsAlive)
                {
                    _follower.Dismiss();
                }
            }

            player.RemoveFollower(bot);

        }

        public bool IsFollower(BotOwner bot, AIBossPlayer boss = null)
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
        public void AddFollowerGroup(int id)
        {
            if (!_botsGroup.Contains(id)) _botsGroup.Add(id);
        }

        public bool IsFollowerGroup(int id)
        {
            return _botsGroup.Contains(id);
        }

        public void removeFollowerGroup(int id)
        {
            if(_botsGroup.Contains(id)) _botsGroup.Remove(id);
        }

        public bool IsBoss(string id)
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

        public List<BotFollowerPlayer> GetBossFollowers(string name)
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

        public List<CustomNavigationPoint> GetCovers()
        {
            return _groupPoints;
        }


        public static List<BotFollowerPlayer> GetFollowersByBoss(string name)
        {
            return Instance.GetBossFollowers(name);
        }

        public static pitAIBossPlayer AddBoss(Player player)
        {
            return Instance.AddBossPlayer(player);
        }

        public static pitAIBossPlayer GetBoss(string name)
        {
            return Instance.GetBossPlayer(name);
        }

        public static List<CustomNavigationPoint> GetAICovers()
        {
            return Instance.GetCovers();
        }

        public static bool IsBossGroup(int it)
        {
            return Instance.IsFollowerGroup(it);
        }

        public static void RemoveFollower(BotOwner bot, pitAIBossPlayer player, bool dismissed = false)
        {
            Instance.RemoveBotFollower(bot, player, dismissed);
        }
    }
}
