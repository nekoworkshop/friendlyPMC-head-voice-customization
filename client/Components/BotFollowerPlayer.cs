using Comfort.Common;
using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using HarmonyLib;

using LootingBots.Patch.Components;
using LootingBots.Patch.Util;
using LootingBots;

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using System.Security.Policy;
using EFT.InventoryLogic;
using static EFT.SpeedTree.TreeWind;
using System.Threading.Tasks;


namespace friendlyPMC.Components
{
    internal class BotFollowerPlayer
    {
        protected BotOwner _bot;
        protected pitAIBossPlayer _player;

        protected BotDifficultySettingsClass _OldSettings;
        protected string _OldGroupID;

        protected LootingBrain _lootingBrain;

        protected TransactionController _transactionController;

        protected LootFinder _lootFinder;

        protected GClass528 settingModif;

        public LootingBrain LootingBrain
        {
            get { return _lootingBrain; }
        }

        public TransactionController TransactionController
        { 
            get { return _transactionController; } 
        }

        public LootFinder LootFinder
        {
            get { return _lootFinder; }
        }

        protected bool _IsSquadMate = false;

        public bool IsSquadMate
        {
            get
            {
                return _IsSquadMate;
            }
        }

       
        protected WildSpawnType _botRole;

        public BotFollowerPlayer(BotOwner bot, pitAIBossPlayer player, bool isSquad = false, WildSpawnType botRole = WildSpawnType.assault)
        {
            _bot = bot;
            _player = player;
            _botRole = botRole == WildSpawnType.assault ? _bot.Profile.Info.Settings.Role : botRole;
            
            _IsSquadMate = isSquad;

            settingModif = new GClass528(1.2f,1.2f,1f,1f,1f,1f,0.9f,1f,1f);

            // change enenmy chooser to custom
            /*if (bot.EnemyChooser != null)
                bot.EnemyChooser.Dispose();

            bot.EnemyChooser = new GClass404(bot);
            bot.EnemyChooser.Activate();*/

            bool hadEnemy = _bot.Memory.HaveEnemy;
            // deactivate old layers

            // force current layer to trigger end decision
            try
            {
                AccessTools.Field(typeof(BaseLogicLayerAbstractClass), "bool_1").SetValue(_bot.Brain.BaseBrain.CurLayerInfo, true);
            } catch { }

            var baseBrain = _bot.Brain.BaseBrain;
            // guess work because we cannot access the private property dictionary_0 where the layers are, but no brain has 20 layers, usually it's 10
            for (int i = 1; i < 20; i++)
            {
                try
                {
                    if(baseBrain != null) baseBrain.method_3(i);
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
            // bot might have request going on, dispose it
            if (_bot.BotRequestController.CurRequest != null)
            {
                _bot.BotRequestController.CurRequest.Complete();
            }
            // bot might have an enemy in his mind, clear it
            if (_bot.Memory.HaveEnemy)
            {
                _bot.Memory.DeleteInfoAboutEnemy(_bot.Memory.GoalEnemy.Person);
            }

            // deactivate old brain
            if (baseBrain != null && baseBrain.CurLayerInfo != null && baseBrain.CurLayerInfo.IsActive)
            {
                string name = baseBrain.CurLayerInfo.Name();
                _bot.Brain.Agent.Deactivate(name);
                baseBrain.CurLayerInfo.IsActive = false;
            }
            _bot.Brain.Agent.Dispose();
            if(baseBrain != null) baseBrain.Dispose();
            _bot.BotsController.AICoreController.Stop();
            _bot.Receiver.Dispose();
            
            // add special follower settings
            SetFollowerSettings(_bot);
            // add a new receiver
            _bot.Receiver = GetFollowerReceiver(bot);
            _bot.Receiver.Init();
            try
            {          
                // initialize LootingBots brain
                _lootingBrain = _bot.GetPlayer.gameObject.GetComponent<LootingBrain>();
                _lootFinder = _bot.GetPlayer.gameObject.GetComponent<LootFinder>();

                _transactionController = AccessTools.Field(typeof(InventoryController), "_transactionController").GetValue(_lootingBrain.InventoryController) as TransactionController;

                _lootingBrain.UpdateGridStats();

            } catch(Exception ex)
            {
                Logger.LogInfo("Failed to add Looting Brain to follower: " +ex.Message);
            }
            // add the new follower brain
            _bot.Brain.BaseBrain = GetFollowerBrain(_bot, _player);
            _bot.Brain.Agent = GetFollowerAIAgent(_bot);
            _bot.BotsController.AICoreController.Activate();
            // let the bot talk
            _bot.BotTalk.SetSilence(0f); 
            // force bot to turn off light
            if(_bot.BotLight != null) _bot.BotLight.TurnOff(false, true);
            //_bot.NightVision.H
            // make bot follower of player
            _player.AddFollower(_bot);
            // activate new following patrol mode
            try
            { 

                var followerAIBase = AccessTools.Field(typeof(PatrolDataFollower), "followerAIBase").GetValue(_bot.BotFollower.PatrolDataFollower) as GClass480;

                if (followerAIBase != null)
                {
                    followerAIBase.Dispose();
                }

                FollowerPatrolInstances.AddPatrol(new FollowerPatrol(player, bot));

                _bot.BotFollower.PatrolDataFollower.IsInited = true;
                _bot.BotFollower.PatrolDataFollower.ManualUpdate();

            } catch(Exception e)
            {
                Logger.LogInfo("Failed to activate new follower patrol mode: " + e.Message);
                Logger.LogInfo("StackTrace : " + e.StackTrace);

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
                    /*// - - any enemy the boss has, the followers will now have
                    try
                    {
                        var _initialBot = AccessTools.Field(typeof(BotsGroup), "_initialBot").GetValue(_player.bossGroup) as BotOwner;
                        _initialBot.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
                        _player.GetEnemies().ForEach(enemy =>
                        {
                            if(enemy.BotState == EBotState.Active && !enemy.IsDead)
                                _player.bossGroup.AddEnemy(enemy.GetPlayer, EBotEnemyCause.initCauseEnemy);
                        });
                    } catch(Exception ex)
                    {
                        Logger.LogInfo(ex.Message);
                    }*/
                }
                else if(_bot.BotsGroup.Id != _player.bossGroup.Id)
                {
                    _bot.BotsGroup.RemoveAlly(_bot);
                    _player.bossGroup.AddMember(_bot, false);
                }
            } else if(_player.bossGroup != null)
            {
                _player.bossGroup.AddMember(_bot, false);
            }

            try
            {
                Weapon primWeapon = bot.AIData.Player.HandsController.Item as Weapon;

                _transactionController.AddExtraAmmo(primWeapon);
            }
            catch (Exception ex)
            {
                Logger.LogInfo("Could not add ammo to follower: " + ex.Message);
            }


            // apply some of settings modifier
            _bot.Settings.Current._hearingDistCoef = settingModif.HearingDistCoef;
            _bot.Settings.Current._precicingSpeedCoef = settingModif.PrecicingSpeedCoef;
            _bot.Settings.Current._accuratySpeedCoef = settingModif.AccuratySpeedCoef;
            _bot.Settings.Current._scatteringCoef = settingModif.ScatteringCoef;

            // reset enemy state
            Utils.Utils.SetTimeout(() =>
            {
                if (_bot != null && !_bot.IsDead && _bot.BotState == EBotState.Active && _bot.Memory.HaveEnemy)
                {
                    _bot.Memory.DeleteInfoAboutEnemy(_bot.Memory.GoalEnemy.Person);
                    _bot.Memory.GoalEnemy = null;
                }
            }, 100f);

            _bot.GetPlayer.BeingHitAction += BeingHitAction;

            Logger.LogInfo($"Bot {_bot.Profile.Nickname} is now a follower of {_player.Player().Profile.Nickname}");

        }
        /** 
         * This is needed since we are disabling CalcGoal for followers 
         * Make the bot look towards the direction he is getting shot it (to see the enemy)
         * **/
        public void BeingHitAction(DamageInfo damageInfo, EBodyPart bodyType, float damageReducedByArmor)
        {
            if(!_bot.Memory.HaveEnemy && damageInfo.Player != null)
            {
                Vector3? pos = damageInfo.Player.iPlayer?.Position;
                if (pos.HasValue)
                {
                    _bot.Steering.LookToPoint((Vector3)pos,90f);
                }
            }
        }

        public virtual FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {
            return new FollowerBrain(bot, boss);
        }

        public virtual AICoreAgentClass<BotLogicDecision> GetFollowerAIAgent(BotOwner bot)
        {
            string name = bot.name + " " + _botRole.ToString();

            return new FollowerAIAgent<BotLogicDecision>(bot.BotsController.AICoreController, bot.Brain.BaseBrain, FollowerCreateNode.ActionsList(bot), bot.gameObject, name, new Func<BotLogicDecision, GClass134>((BotLogicDecision decision) =>
            {
                return FollowerCreateNode.CreateNode(decision, bot);
            }));
        }


        public virtual void SetFollowerSettings(BotOwner bot)
        {
            _OldSettings = _bot.Settings;
            _OldGroupID = _bot.GroupId;
            // increase bot's power
            BotDifficultySettingsClass settings = Singleton<GClass533>.Instance.GetSettings(BotDifficulty.hard, _botRole);
            // - hardcode some settings to make the bot more efficient
            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1f;

            settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = friendlyPMC.enemyRemember.Value;
            settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = 6f;
            settings.FileSettings.Mind.ATTACK_IMMEDIATLY_CHANCE_0_100 = 0f;
            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.CAN_STAND_BY = true;
            settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;
            settings.FileSettings.Mind.CAN_TAKE_ANY_ITEM = true;
            settings.FileSettings.Mind.CAN_TAKE_ITEMS = true;
            settings.FileSettings.Mind.TALK_WITH_QUERY = true;
            settings.FileSettings.Mind.CAN_THROW_REQUESTS = true;
            settings.FileSettings.Mind.CAN_DROP_ITEMS = true;
            settings.FileSettings.Mind.MEDS_ONLY_SAFE_CONTAINER = false;
            settings.FileSettings.Mind.SURGE_KIT_ONLY_SAFE_CONTAINER = false;

            if (_player.realPlayer.Side != EPlayerSide.Savage)
            {
                settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = false;
                settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = true;
            }
            else
            {
                settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = false;
                settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = true;
            }

            settings.FileSettings.Mind.CHANCE_FUCK_YOU_ON_CONTACT_100 = 0;
            settings.FileSettings.Mind.REVENGE_TO_GROUP = true;

            EPlayerSide playerSide = _player.Player().Side;

            // force follower loyality
            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE = playerSide == EPlayerSide.Savage;
            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_BEAR = playerSide == EPlayerSide.Bear;
            settings.FileSettings.Mind.CAN_RECEIVE_PLAYER_REQUESTS_USEC = playerSide == EPlayerSide.Usec;
            settings.FileSettings.Mind.FRIEND_AGR_KILL = 0.000001f;
            settings.FileSettings.Mind.FRIEND_DEAD_AGR_LOW = -0.000001f;
            settings.FileSettings.Mind.REVENGE_FOR_SAVAGE_PLAYERS = false;

            // opposing sides are always enemies
            if (playerSide == EPlayerSide.Bear)
            {
                settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.Attack;
            }
            else if (playerSide == EPlayerSide.Usec)
            {
                settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.Attack;
            }

            if (playerSide != EPlayerSide.Savage)
            {
                settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.Attack;
            }
            else
            {
                settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.Attack;
                settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.Attack;
            }


            // follower can turn enemy to anyone and cares about no one but the boss
            settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
            settings.FileSettings.Mind.REVENGE_BOT_TYPES = new WildSpawnType[] { };
            settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] {
                WildSpawnType.shooterBTR
            };

            settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_VOICE_WHEN_CLOSE = 50;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_GESTURE_WHEN_CLOSE = 100;
            settings.FileSettings.Patrol.CAN_PEACEFUL_LOOK = true;
            settings.FileSettings.Patrol.FRIEND_SEARCH_SEC = 60;
            settings.FileSettings.Patrol.FOLLOWER_START_MOVE_DELAY = 0.5f;
            settings.FileSettings.Patrol.CAN_FRIENDLY_TILT = true;
            settings.FileSettings.Patrol.VISION_DIST_COEF_PEACE = 1f;

            settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = 15f;

            settings.FileSettings.Core.CanGrenade = true;
            settings.FileSettings.Core.CanRun = true;
            settings.FileSettings.Core.VisibleAngle = 160;
            settings.FileSettings.Core.VisibleDistance = 185;
            settings.FileSettings.Core.GainSightCoef = 0.05f;
            settings.FileSettings.Core.ScatteringPerMeter = 0.045f;
            settings.FileSettings.Core.ScatteringClosePerMeter = 0.12f;
            settings.FileSettings.Core.HearingSense = 0.8f;

            settings.FileSettings.Cover.CHECK_CLOSEST_FRIEND = true;

            settings.FileSettings.Aiming.COEF_IF_MOVE = 2f;
            settings.FileSettings.Aiming.MAX_AIM_TIME = 1.5f;
            settings.FileSettings.Aiming.DAMAGE_TO_DISCARD_AIM_0_100 = 100;


            settings.FileSettings.Look.CAN_USE_LIGHT = true;
            settings.FileSettings.Look.FULL_SECTOR_VIEW = false; // seems this makes them too aware of everything around them
            settings.FileSettings.Look.NIGHT_VISION_ON = 75.0f;
            settings.FileSettings.Look.NIGHT_VISION_OFF = 125.0f;
            settings.FileSettings.Look.NIGHT_VISION_DIST = 125.0f;
            settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = 90.0f;
            settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = 5f;
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
            settings.FileSettings.Look.NO_GREEN_DIST = 4.0f;
            settings.FileSettings.Look.NO_GRASS_DIST = 5.0f;

            settings.FileSettings.Hearing.CHANCE_TO_HEAR_SIMPLE_SOUND_0_1 = 0.2f;
            settings.FileSettings.Hearing.DISPERSION_COEF = 1f;
            settings.FileSettings.Hearing.CLOSE_DIST = 6f;
            settings.FileSettings.Hearing.FAR_DIST = 35f;

            

            bot.Settings = settings;
            bot.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
            bot.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);
            // - friendly bot never gets tired
            bot.GetPlayer.Physical.Stamina.ForceMode = true;
            bot.GetPlayer.Physical.HandsStamina.ForceMode = true;
            bot.GetPlayer.HealthController.DisableMetabolism();
            // - have followers share the same groupId as the player
            bot.GetPlayer.Profile.Info.GroupId = _player.realPlayer.GroupId;

            bot.Tactic.AggressionCoef = 1f;

        }

        public FollowerReceiver GetFollowerReceiver(BotOwner bot)
        {
            return new FollowerReceiver(bot);
        }

        public bool IsBot(BotOwner bot)
        {
            return _bot == null ? false : bot.ProfileId == _bot.ProfileId;
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
        /** End Follower Brain **/
        public virtual void Dismiss()
        {
            if (_lootingBrain != null)
            {
                _lootingBrain.StopAllCoroutines();
                _lootingBrain.DisableTransactions();
                _lootingBrain.ActiveItem = null;
                _lootingBrain.ActiveCorpse = null;
            }

            if (_transactionController != null)
                _transactionController = null;

            if (_bot == null || _bot.HealthController.IsAlive) return;

            try
            {

                _bot.BotFollower.PatrolDataFollower.Dispose();
                (_bot.Receiver as FollowerReceiver).Dispose();
                
                _bot.Brain.Dispose();

                _bot.BotsController.AICoreController.Stop();

                // put back old settings
                _bot.Settings = _OldSettings;
                _bot.Profile.Info.GroupId = _OldGroupID;
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

                _bot.GetPlayer.BeingHitAction -= BeingHitAction;

            } catch(Exception ex)
            {
                Logger.LogInfo("Error on dismiss for a follower: " +ex.Message);
            }
            // @TODO : see what else can be reverted
        }
    }
}
