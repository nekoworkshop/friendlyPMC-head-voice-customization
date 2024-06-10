using Comfort.Common;
using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using HarmonyLib;
using LootingBots.Patch.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;

namespace friendlyPMC.Components
{
    internal class BotFollowerPlayer
    {
        private BotOwner _bot;
        private pitAIBossPlayer _player;

        private BotDifficultySettingsClass _OldSettings;

        private LootingBrain _lootingBrain;

        public LootingBrain LootingBrain
        {
            get { return _lootingBrain; }
        }
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
            _bot.Brain.BaseBrain = GetFollowerBrain(_bot, _player);
            _bot.Brain.Agent = GetFollowerAIAgent(_bot);
                
            _bot.BotsController.AICoreController.Activate();

            _bot.BotTalk.SetSilence(0f); // let the bot talk
                

            // make bot follower of player
            _player.OfferBot(_bot);
            // force bot to turn off light
            _bot.BotLight.TurnOff(false, true);
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
                Logger.LogInfo("Failed to activate new follower patrol mode: " + e.Message);
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
                    BossPlayers.Instance.AddFollowerGroup(_player.bossGroup.Id);
                    _player.bossGroup.AddAlly((Player)_player.Player());
                    _player.bossGroup.Lock();
                    _player.bossGroup.OnEnemyAdd += (IPlayer pl, EBotEnemyCause cause) =>
                    {
                        if (pl != null && player.Player().ProfileId == pl.ProfileId)
                        {
                            player.bossGroup.RemoveEnemy(player.Player());
                            player.bossGroup.AddAlly(player.realPlayer);
                        }
                    };
                    _player.bossGroup.AnyBodyShootImmediately = true;

                }
                else
                {
                    _bot.BotsGroup.RemoveAlly(_bot);
                    _player.bossGroup.AddMember(_bot, false);
                }
            }
            // add looting brain to help with pick up items
            _lootingBrain = new LootingBrain();

            Logger.LogInfo($"Bot {_bot.Profile.Nickname} is now a follower of {_player.Player().Profile.Nickname}");

        }

        /** Exposed so that it can be patched by addons **/
        public FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {
            return new FollowerBrain(bot, boss);
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
            _OldSettings = _bot.Settings;
            // increase bot's power
            BotDifficultySettingsClass settings = Singleton<GClass534>.Instance.GetSettings(BotDifficulty.hard, bot.Profile.Info.Settings.Role);
            // - hardcode some settings to make the bot more efficient
            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1f;

            settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = 15f;
            settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = 6f;

            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.CAN_STAND_BY = true;
            settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;
            settings.FileSettings.Mind.CAN_TAKE_ANY_ITEM = true;
            settings.FileSettings.Mind.CAN_TAKE_ITEMS = true;
            settings.FileSettings.Mind.TALK_WITH_QUERY = true;
            settings.FileSettings.Mind.CAN_THROW_REQUESTS = true;

            settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;

            settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = 15f;

            settings.FileSettings.Core.CanGrenade = true;
            settings.FileSettings.Core.CanRun = true;
            settings.FileSettings.Core.VisibleAngle = 160;
            settings.FileSettings.Core.VisibleDistance = 185;
            settings.FileSettings.Core.GainSightCoef = 0.05f;
            settings.FileSettings.Core.ScatteringPerMeter = 0.045f;
            settings.FileSettings.Core.ScatteringClosePerMeter = 0.12f;
            settings.FileSettings.Core.HearingSense = 0.8f;

            settings.FileSettings.Aiming.COEF_IF_MOVE = 2f;
            settings.FileSettings.Aiming.MAX_AIM_TIME = 1.5f;
            settings.FileSettings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = 100;


            settings.FileSettings.Look.CAN_USE_LIGHT = true;
            settings.FileSettings.Look.FULL_SECTOR_VIEW = false;
            settings.FileSettings.Look.MAX_DIST_CLAMP_TO_SEEN_SPEED = 500.0f;
            settings.FileSettings.Look.NIGHT_VISION_ON = 75.0f;
            settings.FileSettings.Look.NIGHT_VISION_OFF = 125.0f;
            settings.FileSettings.Look.NIGHT_VISION_DIST = 125.0f;
            settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = 90.0f;
            settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = 0.0f;
            settings.FileSettings.Look.LightOnVisionDistance = 40.0f;
            settings.FileSettings.Look.VISIBLE_ANG_LIGHT = 30.0f;
            settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT = 50.0f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR = 0.25f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_GREEN = 0.15f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_SHOOT = 0.01f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 1.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_OPT = 1.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE = 4.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE_OPT = 0.25f;
            settings.FileSettings.Look.NO_GREEN_DIST = 3.0f;
            settings.FileSettings.Look.NO_GRASS_DIST = 3.0f;

            bot.Settings = settings;
            bot.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
            bot.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);
            // - friendly bot never gets tired
            bot.GetPlayer.Physical.Stamina.ForceMode = true;
            bot.GetPlayer.Physical.HandsStamina.ForceMode = true;
            bot.GetPlayer.HealthController.DisableMetabolism();


            // refill main weapon
            bot.WeaponManager.Reload.AddAmmoToPockets(bot.WeaponManager.CurrentWeapon.CurrentAmmoTemplate._id, 200);
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
            // end follower brain
            if (_bot == null || _bot.HealthController.IsAlive) return;
            
            _lootingBrain.Cleanup();
            _lootingBrain = null;

            try
            {
                _bot.Brain.Agent.Dispose();
                (_bot.Brain.BaseBrain as FollowerBrain).Dispose();
                (_bot.Receiver as FollowerReceiver).Dispose();

                _bot.BotsController.AICoreController.Stop();

                // put back old settings
                _bot.Settings = _OldSettings;
                _bot.ENEMY_LOOK_AT_ME = Mathf.Cos(_OldSettings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
                _bot.GetPlayer.ActiveHealthController.SetDamageCoeff(_OldSettings.FileSettings.Core.DamageCoeff);

                // add old receiver
                _bot.Receiver = new BotReceiver(_bot);
                _bot.Receiver.Init();

                // add old brain
                _bot.Brain = new StandartBotBrain(_bot);
                _bot.Brain.Activate();

                _bot.BotsController.AICoreController.Activate();

                _bot.GetPlayer.Physical.Stamina.ForceMode = false;
                _bot.GetPlayer.Physical.HandsStamina.ForceMode = false;
                _bot.BotFollower.PatrolDataFollower.Dispose();
            } catch(Exception ex)
            {
                Logger.LogInfo("Error on Dismiss for a follower: " +ex.Message);
            }
            // @TODO : see what else can be reverted
        }
    }
}
