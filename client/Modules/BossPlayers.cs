using EFT;
using friendlyPMC.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Modules
{
    internal class BossPlayers
    {
        public static BossPlayers Instance;

        private Dictionary<string, pitAIBossPlayer> _bosses { get; set; }
        private List<BotFollowerPlayer> _followers { get; set; }
        private List<int> _botsGroup { get; set; }

        private List<CustomNavigationPoint> navigationPoints;

        private List<string> _removedBosses;

        public bool IsDisposed = false;

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
                Logger.LogInfo($"Could not make player {player.Profile.Nickname} as BOSS");
                return null;
            }
            else
            {
                Logger.LogInfo($"Made player {player.Profile.Nickname} a BOSS");
            }

            string name = player.ProfileId;

            if (_removedBosses.Contains(name))
            {
                _removedBosses.Remove(name);
            }

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

        public BotFollowerPlayer AddFollower(BotOwner bot, pitAIBossPlayer player, bool squadMate = false)
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


            bool isAIBoss = false;

            foreach (var item in Utils.Utils.BossFollowersRoles)
            {
                if(bot.IsRole(item))
                {
                    isAIBoss = true;
                    break;
                }
            }

            if (!isAIBoss)
                _follower = new BotFollowerPlayer(bot, player,squadMate);
            else
                _follower = new BossFollowerPlayer(bot, player);

            _followers.Add(_follower);

            return _follower;
        }

        public void RemoveFollower(BotOwner bot, pitAIBossPlayer player,bool dismissed = false)
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

            if (boss != null)
            {
                return bot.BotFollower.HaveBoss && bot.BotFollower.BossToFollow.Player().ProfileId == boss.Player().ProfileId;
            }
            
            if (!bot.BotFollower.HaveBoss) return false;

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

        public BotFollowerPlayer GetFollower(BotOwner bot)
        {
            if (bot == null) return null;
            
            BotFollowerPlayer _follower = null;

            foreach (var item in _followers)
            {
                if (item.IsBot(bot))
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
            return _bosses.ContainsKey(id);
        }

        public pitAIBossPlayer GetBossPlayer(string name)
        {
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
    }
}
