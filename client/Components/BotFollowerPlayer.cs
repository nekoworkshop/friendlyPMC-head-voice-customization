using Comfort.Common;
using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using HarmonyLib;

using System;
using System.Collections.Generic;

using UnityEngine;
using EFT.InventoryLogic;
using System.Linq;

using BepInEx.Bootstrap;

using GridClassEx = GClass2516;
using GridCacheClass = GClass1401;
using static UnityEngine.UI.GridLayoutGroup;

namespace friendlyPMC.Components
{
    internal class BotFollowerPlayer
    {
        protected BotOwner _bot;
        protected pitAIBossPlayer _player;

        protected BotDifficultySettingsClass _OldSettings;
        protected string _OldGroupID;
        protected GClass528 settingModif;

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

            settingModif = new GClass528(1.2f, 1.2f, 1f, 1.2f, 1f, 1f, 1f, 1f, 1f);

            NpcMessage.AddNpc(bot, isSquad);

        }

        public virtual void Init()
        {
            bool hadEnemy = _bot.Memory.HaveEnemy;
            // deactivate old layers

            // force current layer to trigger end decision
            try
            {
                AccessTools.Field(typeof(BaseLogicLayerAbstractClass), "bool_1").SetValue(_bot.Brain.BaseBrain.CurLayerInfo, true);
            }
            catch { }

            var baseBrain = _bot.Brain.BaseBrain;
            // guess work because we cannot access the private property dictionary_0 where the layers are, but no brain has 20 layers, usually it's 10
            for (int i = 1; i < 20; i++)
            {
                try
                {
                    if (baseBrain != null) baseBrain.method_3(i);
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

            // remove looting brain, if present
            if (Chainloader.PluginInfos.ContainsKey("me.skwizzy.lootingbots"))
            {
                Type lootingBrain = Type.GetType("LootingBots.Patch.Components.LootingBrain, LootingBots");

                if (lootingBrain != null)
                {
                    if (_bot.GetPlayer.TryGetComponent(lootingBrain, out Component component))
                    {
                        UnityEngine.Object.Destroy(component);
                    }
                }
            } 

            // deactivate old brain
            if (baseBrain != null && baseBrain.CurLayerInfo != null && baseBrain.CurLayerInfo.IsActive)
            {
                string name = baseBrain.CurLayerInfo.Name();
                _bot.Brain.Agent.Deactivate(name);
                baseBrain.CurLayerInfo.IsActive = false;
            }
            _bot.Brain.Agent.Dispose();
            if (baseBrain != null) baseBrain.Dispose();
            _bot.BotsController.AICoreController.Stop();
            _bot.Receiver.Dispose();

            // add special follower settings
            SetFollowerSettings(_bot);
            // add a new receiver
            _bot.Receiver = GetFollowerReceiver(_bot);
            _bot.Receiver.Init();
            // add the new follower brain
            _bot.Brain.BaseBrain = GetFollowerBrain(_bot, _player);
            _bot.Brain.Agent = GetFollowerAIAgent(_bot);
            _bot.BotsController.AICoreController.Activate();
            // let the bot talk
            _bot.BotTalk.SetSilence(0f);
            // force bot to turn off light
            if (_bot.BotLight != null && _bot.BotLight.IsEnable) _bot.BotLight.TurnOff(false, true);
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

                FollowerPatrolInstances.AddPatrol(new FollowerPatrol(_player, _bot));

                _bot.BotFollower.PatrolDataFollower.IsInited = true;
                _bot.BotFollower.PatrolDataFollower.ManualUpdate();

            }
            catch (Exception e)
            {
                Logger.LogError("Failed to activate new follower patrol mode, fallback to manual mode");
                Logger.LogError(e);

                _bot.BotFollower.PatrolDataFollower.InitPlayer(_player.realPlayer);
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

                    BossPlayers.AddGroupToBoss(_player, _bot.BotsGroup);
                    _player.bossGroup = _bot.BotsGroup;

                    // remove BTR as an enemy for the player group
                    foreach (var item in _player.bossGroup.Enemies)
                    {
                        if (item.Value.Player.Profile.Info.Settings.Role == WildSpawnType.shooterBTR)
                        {
                            _player.bossGroup.RemoveEnemy(item.Value.Player);
                            break;
                        }
                    }
                }
                else if (_bot.BotsGroup.Id != _player.bossGroup.Id)
                {
                    _bot.BotsGroup.RemoveAlly(_bot);
                    _player.bossGroup.AddMember(_bot, false);
                }
            }
            else if (_player.bossGroup != null)
            {
                // do enemy clearing
                foreach (var item in _bot.EnemiesController.EnemyInfos)
                {
                    if (item.Value.Person?.Profile?.Info?.Settings?.Role == WildSpawnType.shooterBTR)
                    {
                        _bot.Memory.DeleteInfoAboutEnemy(item.Value.Person);
                        break;
                    }
                }

                _player.Followers.ForEach(bt => { 
                    if(_bot.EnemiesController.EnemyInfos.TryGetValue(bt, out var info))
                    {
                        _bot.EnemiesController.EnemyInfos.Remove(bt);
                    }
                });

                _player.bossGroup.AddMember(_bot, false);
            }


            // apply some of settings modifier
            _bot.Settings.Current._hearingDistCoef = settingModif.HearingDistCoef;
            _bot.Settings.Current._precicingSpeedCoef = settingModif.PrecicingSpeedCoef;
            _bot.Settings.Current._accuratySpeedCoef = settingModif.AccuratySpeedCoef;
            _bot.Settings.Current._scatteringCoef = settingModif.ScatteringCoef;

            // force  reset enemy state
            Utils.Utils.SetTimeout(() =>
            {
                if (_bot != null && !_bot.IsDead && _bot.BotState == EBotState.Active && _bot.Memory.HaveEnemy)
                {
                    _bot.Memory.DeleteInfoAboutEnemy(_bot.Memory.GoalEnemy.Person);
                    _bot.Memory.GoalEnemy = null;
                }

                // TURN OFF THE FLASHLIGHT!
                if (_bot.BotLight != null && _bot.BotLight.IsEnable)
                {
                    _bot.BotLight.TurnOff(false, true);
                }
            }, 300);

            // ensure bot has enough ammo
            AddExtraAmmo();

            Logger.LogInfo($"Bot {_bot.Profile.Nickname} is now a follower of {_player.Player().Profile.Nickname}");
        }

        protected virtual void SetFollowerSettings(BotOwner bot)
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
            /*settings.FileSettings.Core.VisibleAngle = 160;
            settings.FileSettings.Core.VisibleDistance = 185;
            settings.FileSettings.Core.GainSightCoef = 0.05f;
            settings.FileSettings.Core.ScatteringPerMeter = 0.045f;
            settings.FileSettings.Core.ScatteringClosePerMeter = 0.12f;
            settings.FileSettings.Core.HearingSense = 0.8f;*/

            settings.FileSettings.Cover.CHECK_CLOSEST_FRIEND = true;

            settings.FileSettings.Aiming.COEF_IF_MOVE = 2f;
            settings.FileSettings.Aiming.MAX_AIM_TIME = 1.5f;
            settings.FileSettings.Aiming.SHPERE_FRIENDY_FIRE_SIZE = 0.5f;


            settings.FileSettings.Look.CAN_USE_LIGHT = true;
            settings.FileSettings.Look.FULL_SECTOR_VIEW = true; // seems this makes them aware of everything around them
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
            //settings.FileSettings.Look.LOOK_THROUGH_GRASS = true;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 1.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_OPT = 1.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE = 4.0f;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE_OPT = 0.25f;
            settings.FileSettings.Look.NO_GREEN_DIST = 4.0f;
            settings.FileSettings.Look.NO_GRASS_DIST = 5.0f;

            settings.FileSettings.Hearing.DISPERSION_COEF = 1f;
            settings.FileSettings.Hearing.CLOSE_DIST = 7f;
            settings.FileSettings.Hearing.FAR_DIST = 35f;

            settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING = true;

            bot.Settings = settings;
            bot.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
            bot.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);
            // - friendly bot never gets tired
            bot.GetPlayer.Physical.Stamina.ForceMode = true;
            bot.GetPlayer.Physical.HandsStamina.ForceMode = true;
            bot.GetPlayer.HealthController.DisableMetabolism();
            // - have followers share the same groupId as the player
            bot.GetPlayer.Profile.Info.GroupId = _player.realPlayer.GroupId;
            bot.GetPlayer.Profile.Info.TeamId = _player.realPlayer.Profile.Info.TeamId;

            bot.Tactic.AggressionCoef = 1f;

        }

        protected void AddExtraAmmo()
        {

            InventoryControllerClass inventory = GetInventoryController();
            SearchableItemClass secureContainer;

            try
            {
                secureContainer = (SearchableItemClass)inventory.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;
            }
            catch
            {
                Logger.LogError("Cannot access secure container of bot, extra ammo will not be added");
                return;
            }

            if (secureContainer == null)
            {
                Logger.LogError("Bot has no secure container, cannot add extra ammo");
                return;
            }



            StashGridClass stashGridClass = secureContainer.Grids.FirstOrDefault();

            if (stashGridClass == null)
            {
                return;
            }

            Weapon weapon = _bot.AIData.Player.HandsController.Item as Weapon;

            Item ammoToAdd =
                    weapon.GetCurrentMagazine()?.FirstRealAmmo()
                    ?? Singleton<ItemFactory>.Instance.CreateItem(
                        MongoID.Generate(),
                        weapon.CurrentAmmoTemplate._id,
                        null
                    );

            if (ammoToAdd == null)
            {
                Logger.LogError("Bot has no weapon to add ammo");
                return;
            }

            int ammoAdded = 0;

            for (int i = 0; i < 10; i++)
            {
                Item ammo = ammoToAdd.CloneItem();
                ammo.StackObjectsCount = ammo.StackMaxSize;

                var location = stashGridClass.FindLocationForItem(ammo);

                if (location != null)
                {

                    var result = stashGridClass.AddItemWithoutRestrictions(ammo);

                    if (result.Succeeded)
                    {
                        ammoAdded += ammo.StackObjectsCount;
                        try
                        {
                            Singleton<GridCacheClass>.Instance.Add(
                                        _bot.ProfileId,
                                        location.Grid as GridClassEx,
                                        ammo
                                    );
                        }
                        catch (Exception e)
                        {
                            Components.Logger.LogError(e);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

        }

        public InventoryControllerClass GetInventoryController()
        {
            return _bot.GetPlayer.InventoryControllerClass;
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
        
        public virtual void Dismiss()
        {
            if (_bot == null) return;
            try
            {
                // these 2 are automatically called if bot dies or leaves
                // we call them here in the case bot is still alive but has been dismissed
                _bot.BotFollower.PatrolDataFollower.Dispose();
                (_bot.Receiver as FollowerReceiver).Dispose();
                // turn off follower brain
                (_bot.Brain.BaseBrain as FollowerBrain).Dismissed();

                if (_bot.IsDead || _bot.BotState != EBotState.Active) return;

                _bot.Brain.Dispose();

                _bot.BotsController.AICoreController.Stop();

                // put back old settings
                _bot.Settings = _OldSettings;
                _bot.Profile.Info.GroupId = _OldGroupID;
                _bot.ENEMY_LOOK_AT_ME = Mathf.Cos(_OldSettings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
                _bot.GetPlayer.ActiveHealthController.SetDamageCoeff(_OldSettings.FileSettings.Core.DamageCoeff);

                // put back old receiver
                _bot.Receiver = new BotReceiver(_bot);
                _bot.Receiver.Init();

                // put back old brain
                _bot.Brain = new StandartBotBrain(_bot);
                _bot.Brain.Activate();

                _bot.BotsController.AICoreController.Activate();

                _bot.GetPlayer.Physical.Stamina.ForceMode = false;
                _bot.GetPlayer.Physical.HandsStamina.ForceMode = false;

            }
            catch (Exception ex)
            {
                Logger.LogInfo("Error on dismiss for a follower: " + ex.Message);
                Logger.LogInfo(ex.StackTrace);
            }
            // @TODO : see what else can be reverted
        }
    }
}
