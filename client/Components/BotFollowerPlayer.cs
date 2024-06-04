using Comfort.Common;
using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class BotFollowerPlayer
    {
        private BotOwner _bot;
        private pitAIBossPlayer _player;
        public BotFollowerPlayer(BotOwner bot, pitAIBossPlayer player)
        {
            _bot = bot;
            _player = player;


            // deactivate old layers
            var baseBrain = _bot.Brain.BaseBrain;
            // guess work because we cannot access the private property dictionary_0 where the layers are, but no brain has 20 layers, usually it's 10
            for (int i = 1; i < 20; i++)
            {
                try
                {
                    _bot.Brain.BaseBrain.method_3(i);
                }
                catch (Exception)
                {

                }
            }

            // bot might be following someone, reset that
            if (_bot.BotFollower.HaveBoss)
            {
                _bot.BotFollower.BossToFollow.RemoveFollower(_bot);
                _bot.BotFollower.BossToFollow = null;
            }

            // deactivate old brain
            if (_bot.Brain.BaseBrain.CurLayerInfo != null && _bot.Brain.BaseBrain.CurLayerInfo.IsActive)
            {
                string name = _bot.Brain.BaseBrain.CurLayerInfo.Name();
                _bot.Brain.Agent.Deactivate(name);
                _bot.Brain.BaseBrain.CurLayerInfo.IsActive = false;
            }
            _bot.Brain.Agent.Dispose();
            _bot.Brain.BaseBrain.Dispose();
            _bot.BotsController.AICoreController.Stop();
            
            _bot.Receiver.Dispose();

            // add special follower settings
            SetlFollowerSettings(_bot);

            // add a new receiver
            _bot.Receiver = GetFollowerReceiver(bot);
            _bot.Receiver.Init();

            // add the new follower brain
            _bot.Brain.BaseBrain = GetFollowerBrain(_bot);
            _bot.Brain.Agent = GetFollowerAIAgent(_bot);
                
            _bot.BotsController.AICoreController.Activate();

            _bot.BotTalk.SetSilence(0f); // let the bot talk
                

            // make bot follower of player
            _player.OfferBot(_bot);
            _bot.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Protect);
            (_bot.Brain.BaseBrain as FollowerBrain).SetBossTactic();

            // activate new following patrol mode
            try
            { 

                var followerAIBase = AccessTools.Field(typeof(PatrolDataFollower), "followerAIBase").GetValue(_bot.BotFollower.PatrolDataFollower) as GClass480;

                if (followerAIBase != null)
                {
                    followerAIBase.Dispose();
                }

                FollowerPatrolInstances.AddPatrol(new FollowerPatrol(player.realPlayer, bot));

                _bot.BotFollower.PatrolDataFollower.IsInited = true;
                _bot.BotFollower.PatrolDataFollower.ManualUpdate();

            } catch(Exception e)
            {
                Logger.LogInfo("Could not activate new follower patrol mode : " + e.Message);
                _bot.BotFollower.PatrolDataFollower.InitPlayer(player.realPlayer);
                if (!_bot.BotFollower.PatrolDataFollower.IsInited)
                {
                    _bot.BotFollower.PatrolDataFollower.IsInited = true;
                }
                _bot.BotFollower.PatrolDataFollower.ManualUpdate();
            }

             // make all followers have the same group
             if (_bot.BotsGroup != null)
             {
                // - if there is no group yet, take the bot's group
                if (_player.bossGroup == null)
                {
                    int count = _bot.BotsGroup.MembersCount;
                    List<BotOwner> membersToRemove = new List<BotOwner>();
                    for (int i = 0; i < count; i++)
                    {
                        BotOwner member = _bot.BotsGroup.Member(i);
                        if (member.ProfileId != _bot.ProfileId)
                        {
                            membersToRemove.Add(member);
                        }
                    }
                    membersToRemove.ForEach(mem =>
                    {
                        _bot.BotsGroup.RemoveAlly(mem);
                    });

                    _player.bossGroup = _bot.BotsGroup;
                    _player.bossGroup.AddAlly((Player)_player.Player());
                    _player.bossGroup.Lock();
                    _player.bossGroup.OnEnemyAdd += OnAddEnemyGroup;
                    _player.bossGroup.AnyBodyShootImmediately = true;

                }
                else
                {
                    _bot.BotsGroup.RemoveAlly(_bot);
                    _player.bossGroup.AddMember(_bot, false);
                }
            }


            _bot.GetPlayer.HealthController.DiedEvent += OnDead;
            _bot.LeaveData.OnLeave += OnLeave;
            _bot.Memory.OnAddEnemy += OnAddEnemy;

            Logger.LogInfo($"Bot {_bot.Profile.Nickname} is now a follower of {_player.Player().Profile.Nickname}");

        }

        public void ClearFollowerPatrol(BotOwner bot)
        {
            var patrols = FollowerPatrolInstances.GetPatrols();
            foreach (var item in patrols)
            {
                if (item.botOwner.ProfileId == bot.ProfileId)
                {
                    patrols.Remove(item);
                    break;
                }
            }
        }

        /** Exposed so that it can be patched by addons **/
        public void OnDead(EDamageType damageType)
        {
            BossPlayers.Instance.RemoveFollower(_bot, _player);
            ClearFollowerPatrol(_bot);

        }
        /** Exposed so that it can be patched by addons **/
        public void OnLeave(BotOwner _bot)
        {
            BossPlayers.Instance.RemoveFollower(_bot, _player);
            ClearFollowerPatrol(_bot);
        }
        // how does the boss get added as Enemy?? - fix it
        public void OnAddEnemy(IPlayer player)
        {
            
            if(player != null && player == _player) {
                _bot.Memory.DeleteInfoAboutEnemy(player);
                _bot.BotsGroup.RemoveEnemy(player);
                _bot.BotsGroup.AddAlly((Player)_player.Player());
            }
        }
        // how does the boss get added as Enemy Group?? - fix it
        public void OnAddEnemyGroup(IPlayer player, EBotEnemyCause cause)
        {
            if (player != null && player == _player)
            {
                _bot.BotsGroup.RemoveEnemy(_player.Player());
                _bot.BotsGroup.AddAlly((Player)_player.Player());
            }
        }

        /** Exposed so that it can be patched by addons **/
        public FollowerBrain GetFollowerBrain(BotOwner bot)
        {
            return new FollowerBrain(bot);
        }
        /** Exposed so that it can be patched by addons **/
        public AICoreAgentClass<BotLogicDecision> GetFollowerAIAgent(BotOwner bot)
        {
            string name = bot.name + " " + bot.Profile.Info.Settings.Role.ToString();

            return new AICoreAgentClass<BotLogicDecision>(bot.BotsController.AICoreController, bot.Brain.BaseBrain, GClass460.ActionsList(bot), bot.gameObject, name, new Func<BotLogicDecision, GClass134>(bot.Brain.method_0));
        }
        
        /** Exposed so that it can be patched by addons **/
        public void SetlFollowerSettings(BotOwner bot)
        {
            // increase bot's power
            BotDifficultySettingsClass settings = Singleton<GClass534>.Instance.GetSettings(BotDifficulty.hard, bot.Profile.Info.Settings.Role);
            // - hardcode some settings to make the bot more efficient
            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1f;
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = 20f;

            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.CAN_STAND_BY = true;
            settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;
            settings.FileSettings.Mind.CAN_TAKE_ANY_ITEM = true;
            settings.FileSettings.Mind.CAN_TAKE_ITEMS = true;
            settings.FileSettings.Mind.TALK_WITH_QUERY = true;
            settings.FileSettings.Mind.CAN_THROW_REQUESTS = true;

            settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;

            settings.FileSettings.Core.CanGrenade = true;
            settings.FileSettings.Core.CanRun = true;

            bot.Settings = settings;
            bot.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
            bot.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);
            // - friendly bot never gets tired
            bot.GetPlayer.Physical.Stamina.ForceMode = true;
            bot.GetPlayer.Physical.HandsStamina.ForceMode = true;
            bot.GetPlayer.HealthController.DisableMetabolism();
            // - give bot perma medkits
            /*bot.Medecine.FirstAid.Dispose();
            bot.Medecine.FirstAid = new FollowerMeds(bot, new Action<bool>(bot.Medecine.method_0));
            bot.Medecine.SurgicalKit.Dispose();
            bot.Medecine.SurgicalKit = new GClass416(bot, new Action<bool>(bot.Medecine.method_0));
            bot.Medecine.FirstAid.Activate();
            bot.Medecine.SurgicalKit.Activate();*/

            // refill weapons
            bot.WeaponManager.Reload.AddAmmoToPockets(bot.WeaponManager.CurrentWeapon.CurrentAmmoTemplate._id, 100);
        }

        /** Exposed so that it can be patched by addons **/
        public FollowerReceiver GetFollowerReceiver(BotOwner bot)
        {
            return new FollowerReceiver(bot);
        }

        public bool IsBot(BotOwner bot)
        {
            return bot == _bot;
        }

        public BotOwner GetBot()
        {
            return _bot;
        }

        public pitAIBossPlayer GetBoss()
        {
            if (_bot == null) return null;
            return _player;
        }

        public void Dismiss()
        {
            _bot.Receiver.Dispose();
            _bot.Receiver = new BotReceiver(_bot);
            _bot.Receiver.Init();
            
            _bot.GetPlayer.HealthController.DiedEvent -= OnDead;
            _bot.LeaveData.OnLeave -= OnLeave;
            _bot.Memory.OnAddEnemy -= OnAddEnemy;
            
            if(_bot.BotsGroup != null)
                _bot.BotsGroup.OnEnemyAdd -= OnAddEnemyGroup;

            _bot.GetPlayer.Physical.Stamina.ForceMode = false;
            _bot.GetPlayer.Physical.HandsStamina.ForceMode = false;
            _bot.BotFollower.PatrolDataFollower.Dispose();
            // @TODO : see what else can be reverted
        }
    }
}
