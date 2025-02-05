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

using DrakiaXYZ.BigBrain.Brains;
using System.Reflection;
using friendlyPMC.Patches;

namespace friendlyPMC.Components
{
    public class BotFollowerPlayer
    {
        protected BotOwner _bot;
        protected pitAIBossPlayer _player;

        protected GClass580 settingModif;

        protected bool _IsSquadMate = false;

        protected string _grouId;
        protected string _teamId;

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

            settingModif = new GClass580(1.2f, 1.2f, 1f, 1.1f, 1f, 1f, 1.2f, 1f, 1f);

            NpcMessage.AddNpc(bot, isSquad);

        }

        public virtual void Init()
        {

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

            // reset bot animation stances
            _bot.GetPlayer.MovementContext.SetPatrol(false);
            _bot.Tilt.Stop();

            if (SAINPatch.IsSAINInstalled() && !_IsSquadMate)
            {
                Type BotController = Type.GetType("SAIN.Components.BotController.BotSpawnController, SAIN");

                if (BotController != null)
                {
                    var botSpawnControllerProperty = BotController.GetField("Instance", BindingFlags.Public | BindingFlags.Static);

                    if (botSpawnControllerProperty != null)
                    {
                        object botSpawnControllerInstance = botSpawnControllerProperty.GetValue(null);

                        if (botSpawnControllerInstance != null)
                        {
                            // Find the removeBot method
                            var removeBotMethod = BotController.GetMethod("removeBot", BindingFlags.Public | BindingFlags.Instance);

                            if (removeBotMethod != null)
                            {
                                // Call the method if you have the bot instance to pass
                                removeBotMethod.Invoke(botSpawnControllerInstance, new object[] { _bot });

                                Modules.Logger.LogInfo("SAIN brain disabled for the bot.");
                            }
                            else
                            {
                                Modules.Logger.LogInfo("Could not find the 'removeBot' method.");
                            }
                        }
                        else
                        {
                            Modules.Logger.LogInfo("Could not retrieve the BotSpawnController instance.");
                        }
                    }
                    else
                    {
                        Modules.Logger.LogInfo("Could not find the 'Instance' property on BotSpawnController.");
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
            _bot.BotState = EBotState.NonActive;
            _bot.Receiver.Dispose();


            // add special follower settings
            SetFollowerSettings(_bot);
            // add a new receiver
            _bot.Receiver = GetFollowerReceiver(_bot);
            _bot.Receiver.Init();
            // add the new follower brain
            _bot.Brain.BaseBrain = GetFollowerBrain(_bot, _player);
            _bot.Brain.Agent = GetFollowerAIAgent(_bot);
            _bot.BotState = EBotState.Active;
            // let the bot talk
            _bot.BotTalk.SetSilence(0f);
            // force bot to turn off light
            if (_bot.BotLight != null && _bot.BotLight.IsEnable) _bot.BotLight.TurnOff(false, true);
            // make bot follower of player
            _player.AddFollower(_bot);
            // make bot join the player's group
            if (_player.bossGroup != null)
            {
                // clear the player's followers from being enemies to the bot
                _player.Followers.ForEach(bt =>
                {
                    if (_bot.EnemiesController.EnemyInfos.TryGetValue(bt, out var fl))
                    {
                        _bot.EnemiesController.EnemyInfos.Remove(bt);
                    }
                });
                // clear the player from being an enemy to the bot
                if (_bot.EnemiesController.EnemyInfos.TryGetValue(_player.realPlayer, out var info))
                {
                    _bot.EnemiesController.EnemyInfos.Remove(_player.realPlayer);
                }
                // add the bot to the player's group, if not already (PickUp case here with spawn)
                if (_bot.BotsGroup.Id != _player.bossGroup.Id)
                {
                    _bot.BotsGroup.RemoveAlly(_bot);
                    // - ensure the bot is not marked as enemy already by the others
                    _player.bossGroup.RemoveEnemy(_bot.GetPlayer);

                    _bot.BotsGroup = _player.bossGroup;
                    var botsGroupField = AccessTools.Field(typeof(BotMemoryClass), "botsGroup_0");
                    botsGroupField.SetValue(_bot.Memory, _bot.BotsGroup);

                    var _groupRequestController = AccessTools.Field(typeof(BotRequestController), "_groupRequestController");
                    (_groupRequestController.GetValue(_bot.BotRequestController) as BotGroupRequestController).OnAddRequest -= _bot.BotRequestController.method_0;
                    _groupRequestController.SetValue(_bot.BotRequestController, null);

                    var botEnemies = _bot.EnemiesController.EnemyInfos.ToList();
                    foreach (var item in botEnemies)
                    {
                        _bot.Memory.DeleteInfoAboutEnemy(item.Key);
                    }

                    _player.bossGroup.AddMember(_bot, false);
                    foreach (var en in _player.bossGroup.Enemies)
                    {
                        _bot.Memory.AddEnemy(en.Key, en.Value, false);
                    }
                }
            }
            // if there is no group yet, make one and group the player with the bot (PickUp case here without spawn)
            else
            {
                _bot.BotsGroup.RemoveAlly(_bot);

                var botsGroupField = AccessTools.Field(typeof(BotMemoryClass), "botsGroup_0");
                var _groupRequestController = AccessTools.Field(typeof(BotRequestController), "_groupRequestController");
                (_groupRequestController.GetValue(_bot.BotRequestController) as BotGroupRequestController).OnAddRequest -= _bot.BotRequestController.method_0;
                _groupRequestController.SetValue(_bot.BotRequestController, null);

                _bot.Settings.GetEnemyBotTypes().RemoveAll(x => Utils.Props.friendlyBotTypes.Contains(x));
                _bot.Settings.GetFriendlyBotTypes().AddRange(Utils.Props.friendlyBotTypes);

                BotZone zone = _bot.BotsController.BotSpawner.GetClosestZone(_bot.GetPlayer.Transform.position, out var zoneDist);
                BotsGroup group = _bot.BotsController.BotSpawner.GetGroupAndSetEnemies(_bot, zone);

                _bot.BotsGroup = group;
                botsGroupField.SetValue(_bot.Memory, group);
                _groupRequestController.SetValue(_bot.BotRequestController, group.RequestsController);

                // - go through the enemy filtering process
                var groupEnemies = _bot.BotsGroup.Enemies;
                var botEnemies = _bot.EnemiesController.EnemyInfos.ToList();
                foreach (var item in botEnemies)
                {
                    _bot.Memory.DeleteInfoAboutEnemy(item.Key);
                }

                group.AddMember(_bot, false);
                BossPlayers.AddGroupToBoss(_player, group);

                _bot.Memory.GoalEnemy = null;
            }

            // apply the settings modifier
            _bot.Settings.Current._hearingDistCoef = settingModif.HearingDistCoef;
            _bot.Settings.Current._precicingSpeedCoef = settingModif.PrecicingSpeedCoef;
            _bot.Settings.Current._accuratySpeedCoef = settingModif.AccuratySpeedCoef;
            _bot.Settings.Current._scatteringCoef = settingModif.ScatteringCoef;
            _bot.Settings.Current._visibleDistCoef = settingModif.VisibleDistCoef;
            _bot.Settings.Current._layChanceDangerCoef = settingModif.LayChanceDangerCoef;
            _bot.Settings.Current._priorityScatteringCoef = settingModif.PriorityScatteringCoef;
            _bot.Settings.Current._gainSightCoef = settingModif.GainSightCoef;
            _bot.Settings.Current._triggerDownDelay = settingModif.TriggerDownDelay;


            Utils.Utils.SetTimeout(() =>
            {
                // TURN OFF THE FLASHLIGHT!
                if (_bot.BotLight != null && _bot.BotLight.IsEnable)
                {
                    _bot.BotLight.TurnOff(false, false);
                }
            }, 300);

            // ensure bot has enough ammo
            AddExtraAmmo();

            // - ensure weapon is in auto mode
            if (_bot.WeaponManager.ShootController.Item != null && _bot.WeaponManager.ShootController.Item.WeapFireType.Contains(Weapon.EFireMode.fullauto))
                _bot.WeaponManager.ShootController.ChangeFireMode(Weapon.EFireMode.fullauto);

            Modules.Logger.LogInfo($"Bot {_bot.Profile.Nickname} is now a follower of {_player.Player().Profile.Nickname}");
        }

        protected virtual void SetFollowerSettings(BotOwner bot)
        {
            // increase bot's power
            BotDifficultySettingsClass settings = Singleton<GClass585>.Instance.GetSettings(BotDifficulty.hard, _botRole);

            // - hardcode some settings to make the bot more efficient
            settings.FileSettings.Move.REACH_DIST = 1.5f;
            settings.FileSettings.Move.REACH_DIST_COVER = 2f;
            settings.FileSettings.Move.REACH_DIST_RUN = 1f;
            settings.FileSettings.Boss.BIG_PIPE_ARTILLERY_COUNT = 1;

            settings.FileSettings.Mind.DIST_TO_STOP_RUN_ENEMY = 15f;
            settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = friendlyPMC.enemyRemember.Value;
            settings.FileSettings.Mind.TIME_TO_FIND_ENEMY = 6f;
            settings.FileSettings.Mind.ATTACK_IMMEDIATLY_CHANCE_0_100 = 0f;
            settings.FileSettings.Mind.CAN_TALK = true;
            settings.FileSettings.Mind.CAN_STAND_BY = true;
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
            settings.FileSettings.Mind.CAN_EXECUTE_REQUESTS = true;

            settings.FileSettings.Mind.FRIEND_AGR_KILL = 0.000001f;
            settings.FileSettings.Mind.FRIEND_DEAD_AGR_LOW = -0.000001f;
            settings.FileSettings.Mind.REVENGE_FOR_SAVAGE_PLAYERS = false;

            // opposing sides are always enemies
            if (playerSide == EPlayerSide.Bear)
            {
                settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            }
            else if (playerSide == EPlayerSide.Usec)
            {
                settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            }

            if (playerSide != EPlayerSide.Savage)
            {
                settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            }
            else
            {
                settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
                settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.AlwaysEnemies;
            }


            // follower can turn enemy to anyone and cares the boss
            settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
            settings.FileSettings.Mind.REVENGE_BOT_TYPES = new WildSpawnType[] { };
            settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = Utils.Props.friendlyBotTypes.ToArray();

            settings.FileSettings.Patrol.PICKUP_ITEMS_TO_BACKPACK_OR_CONTAINER = true;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_VOICE_WHEN_CLOSE = 50;
            settings.FileSettings.Patrol.CHANCE_TO_PLAY_GESTURE_WHEN_CLOSE = 100;
            settings.FileSettings.Patrol.CAN_PEACEFUL_LOOK = true;
            settings.FileSettings.Patrol.FRIEND_SEARCH_SEC = 60;
            settings.FileSettings.Patrol.FOLLOWER_START_MOVE_DELAY = 0.5f;
            settings.FileSettings.Patrol.CAN_FRIENDLY_TILT = true;
            settings.FileSettings.Patrol.VISION_DIST_COEF_PEACE = 1f;

            settings.FileSettings.Boss.SHALL_WARN = false;
            settings.FileSettings.Patrol.MAX_YDIST_TO_START_WARN_REQUEST_TO_REQUESTER = 0f;

            settings.FileSettings.Look.MINIMUM_VISIBLE_DIST = 15f;

            settings.FileSettings.Core.CanGrenade = friendlyPMC.botGrenades.Value;
            settings.FileSettings.Core.CanRun = true;
            settings.FileSettings.Core.AccuratySpeed = 0.25f;

            settings.FileSettings.Cover.CHECK_CLOSEST_FRIEND = true;
            settings.FileSettings.Cover.DOG_FIGHT_AFTER_LEAVE = 1;
            settings.FileSettings.Cover.HIDE_TO_COVER_TIME = 5;
            settings.FileSettings.Cover.HITS_TO_LEAVE_COVER = 2;
            settings.FileSettings.Cover.HITS_TO_LEAVE_COVER_UNKNOWN = 2;
            settings.FileSettings.Cover.TIME_TO_MOVE_TO_COVER = 15;
            settings.FileSettings.Cover.RETURN_TO_ATTACK_AFTER_AMBUSH_MIN = 20;
            settings.FileSettings.Cover.RETURN_TO_ATTACK_AFTER_AMBUSH_MAX = 50;
            settings.FileSettings.Cover.SPOTTED_GRENADE_RADIUS = 24f;
            settings.FileSettings.Cover.SPOTTED_GRENADE_TIME = 7;

            settings.FileSettings.Aiming.COEF_IF_MOVE = 1f;
            settings.FileSettings.Aiming.MAX_AIM_TIME = 1.5f;
            settings.FileSettings.Aiming.SHPERE_FRIENDY_FIRE_SIZE = 0.5f;
            settings.FileSettings.Aiming.AIMING_TYPE = 6; // the head is a priority
            settings.FileSettings.Aiming.ANY_PART_SHOOT_TIME = 0.3f; // what is this, what does it do?
            settings.FileSettings.Aiming.ANYTIME_LIGHT_WHEN_AIM_100 = 70;
            settings.FileSettings.Aiming.BAD_SHOOTS_MAX = 3;
            settings.FileSettings.Aiming.BAD_SHOOTS_MIN = 1;
            settings.FileSettings.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.20f;


            settings.FileSettings.Look.CAN_USE_LIGHT = true;
            //settings.FileSettings.Look.FULL_SECTOR_VIEW = true; // seems this makes them aware of everything around them
            settings.FileSettings.Look.NIGHT_VISION_ON = 100.0f;
            settings.FileSettings.Look.NIGHT_VISION_OFF = 110.0f;
            settings.FileSettings.Look.NIGHT_VISION_DIST = 160.0f;
            settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION = 120f;
            settings.FileSettings.Look.LOOK_THROUGH_PERIOD_BY_HIT = 5f;
            settings.FileSettings.Look.LightOnVisionDistance = 40.0f;
            settings.FileSettings.Look.LOOK_LAST_POSENEMY_IF_NO_DANGER_SEC = 25f;
            settings.FileSettings.Look.VISIBLE_ANG_LIGHT = 45.0f;
            settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT = 65.0f;

            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR = 0.25f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_GREEN = 0.15f;
            settings.FileSettings.Look.GOAL_TO_FULL_DISSAPEAR_SHOOT = 0.01f;
            //settings.FileSettings.Look.LOOK_THROUGH_GRASS = true;
            settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 1.0f;
            settings.FileSettings.Look.NO_GREEN_DIST = 4.0f;
            settings.FileSettings.Look.NO_GRASS_DIST = 5.0f;

            settings.FileSettings.Hearing.DISPERSION_COEF = 1.6f;
            settings.FileSettings.Hearing.CLOSE_DIST = 7f;
            settings.FileSettings.Hearing.FAR_DIST = 35f;

            settings.FileSettings.Cover.SIT_DOWN_WHEN_HOLDING = true;

            bot.Settings = settings;

            bot.ENEMY_LOOK_AT_ME = Mathf.Cos(settings.FileSettings.Mind.ENEMY_LOOK_AT_ME_ANG * 0.017453292f);
            bot.GetPlayer.ActiveHealthController.SetDamageCoeff(settings.FileSettings.Core.DamageCoeff);

            // - friendly bot never gets tired
            bot.GetPlayer.Physical.Stamina.ForceMode = true;
            bot.GetPlayer.Physical.HandsStamina.ForceMode = true;
            // - need no food
            bot.GetPlayer.HealthController.DisableMetabolism();
            // - and blackout does not affect them
            bot.GetPlayer.ActiveHealthController.DoPainKiller();
            // - have followers share the same groupId as the player
            _grouId = bot.GetPlayer.Profile.Info.GroupId;
            _teamId = bot.GetPlayer.Profile.Info.TeamId;
            bot.GetPlayer.Profile.Info.GroupId = _player.realPlayer.GroupId;
            bot.GetPlayer.Profile.Info.TeamId = _player.realPlayer.Profile.Info.TeamId;

            bot.Tactic.AggressionCoef = 1f;

            // - take on the new vision values
            AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE").SetValue(bot.LookSensor, Mathf.Cos(settings.FileSettings.Core.VisibleAngle * 0.017453292f));
            AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE_LIGHT").SetValue(bot.LookSensor, Mathf.Cos(settings.FileSettings.Look.VISIBLE_ANG_LIGHT * 0.017453292f));
            AccessTools.Field(typeof(LookSensor), "VISIBLE_ANGLE_NIGHTVISION").SetValue(bot.LookSensor, Mathf.Cos(settings.FileSettings.Look.VISIBLE_ANG_NIGHTVISION * 0.017453292f));

            _bot.LookSensor.UpdateLook();

        }

        protected void AddExtraAmmo()
        {

            InventoryController inventory = GetInventoryController();
            SearchableItemItemClass secureContainer;

            try
            {
                secureContainer = (SearchableItemItemClass)inventory.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;
            }
            catch
            {
                Modules.Logger.LogError("Cannot access secure container of bot, extra ammo will not be added");
                return;
            }

            if (secureContainer == null)
            {
                Modules.Logger.LogError("Bot has no secure container, cannot add extra ammo");
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
                    ?? Singleton<ItemFactoryClass>.Instance.CreateItem(
                        MongoID.Generate(),
                        weapon.CurrentAmmoTemplate._id,
                        null
                    );

            if (ammoToAdd == null)
            {
                Modules.Logger.LogError("Bot has no weapon to add ammo");
                return;
            }

            int ammoAdded = 0;

            for (int i = 0; i < 10; i++)
            {
                Item ammo = ammoToAdd.CloneItem();
                ammo.StackObjectsCount = ammo.StackMaxSize;

                var location = stashGridClass.FindFreeSpace(ammo);

                if (location != null)
                {
                    var result = stashGridClass.AddItemWithoutRestrictions(ammo, location);

                    if (result.Succeeded)
                    {
                        ammoAdded += ammo.StackObjectsCount;
                    }
                    else
                    {
                        Modules.Logger.LogError("Failed to add ammo to bot's secure container");
                        break;
                    }
                }
                else
                {
                    Modules.Logger.LogInfo("No more space in secure container for ammo");
                    break;
                }
            }

        }

        public InventoryController GetInventoryController()
        {
            return _bot.GetPlayer.InventoryController;
        }

        public virtual FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {
            return new FollowerBrain(bot, boss);
        }

        public virtual AICoreAgentClass<BotLogicDecision> GetFollowerAIAgent(BotOwner bot)
        {
            string name = bot.name + " " + _botRole.ToString();

            return new FollowerAIAgent<BotLogicDecision>(bot.BotsController.AICoreController, bot.Brain.BaseBrain, FollowerCreateNode.ActionsList(bot), bot.gameObject, name, new Func<BotLogicDecision, GClass156>((BotLogicDecision decision) =>
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

        public virtual void Dismiss(bool warnPlayer = false)
        {
            if (_bot == null) return;
            try
            {
                // these 2 are automatically called if bot dies or leaves
                // we call them here in the case bot is still alive but has been dismissed
                (_bot.Receiver as FollowerReceiver).Dispose();
                // turn off follower brain
                (_bot.Brain.BaseBrain as FollowerBrain).Dismissed();

                if (_bot.IsDead || _bot.BotState != EBotState.Active) return;

                _bot.BotState = EBotState.NonActive;

                // - ensure bot no longer follows the player
                if (_bot.BotFollower.HaveBoss)
                {
                    _bot.BotFollower.BossToFollow.RemoveFollower(_bot);
                    _bot.BotFollower.BossToFollow = null;

                }
                // - bot might have request going on, dispose it
                if (_bot.BotRequestController.CurRequest != null)
                {
                    _bot.BotRequestController.CurRequest.Complete();
                }

                // remove follower brain
                _bot.Brain.Dispose();
                _bot.BotsController.AICoreController.Stop();

                // - put back old receiver
                _bot.Receiver = new BotReceiver(_bot);
                _bot.Receiver.Init();


                // put back old brain
                // - first BigBrain has to know the bot is no longer active
                var brainManagerInstance = AccessTools.Property(typeof(BrainManager), "Instance");
                bool bigBraidDeactivated = false;
                if (brainManagerInstance != null)
                {
                    BrainManager manager = (BrainManager)brainManagerInstance.GetValue(null);
                    if (manager != null)
                    {
                        var activatedBotsProperty = AccessTools.Field(typeof(BrainManager), "ActivatedBots");
                        if (activatedBotsProperty != null)
                        {
                            var activatedBots = (Dictionary<IPlayer, BotOwner>)activatedBotsProperty.GetValue(manager);
                            if (activatedBots != null)
                            {
                                activatedBots.Remove(_bot.GetPlayer);
                                bigBraidDeactivated = true;
                            }
                        }
                    }
                }
                // - now put back the old brain
                if (bigBraidDeactivated)
                {
                    _bot.Brain = new StandartBotBrain(_bot);
                    _bot.Brain.Activate();
                }

                _bot.BotsController.AICoreController.Activate();

                _bot.BotState = EBotState.Active;

                _bot.GetPlayer.Physical.Stamina.ForceMode = false;
                _bot.GetPlayer.Physical.HandsStamina.ForceMode = false;

                // -- bot needs a new group
                BotZone zone = _bot.BotsController.BotSpawner.GetClosestZone(_bot.GetPlayer.Transform.position, out var zoneDist);
                BotsGroup group = _bot.BotsController.BotSpawner.GetGroupAndSetEnemies(_bot, zone);

                _bot.BotsGroup = group;

                var botsGroupField = AccessTools.Field(typeof(BotMemoryClass), "botsGroup_0");
                var _groupRequestController = AccessTools.Field(typeof(BotRequestController), "_groupRequestController");
                (_groupRequestController.GetValue(_bot.BotRequestController) as BotGroupRequestController).OnAddRequest -= _bot.BotRequestController.method_0;
                _groupRequestController.SetValue(_bot.BotRequestController, null);

                botsGroupField.SetValue(_bot.Memory, group);
                _groupRequestController.SetValue(_bot.BotRequestController, group.RequestsController);

                _bot.GetPlayer.Profile.Info.GroupId = _grouId;
                _bot.GetPlayer.Profile.Info.TeamId = _teamId;

                _bot.BotsGroup.AddMember(_bot, false);

                _bot.Memory.IsPeace = !warnPlayer;

                // make player and his followers enemies of the bot
                if (warnPlayer)
                {
                    _player.Followers.ForEach(fl =>
                    {
                        if (_bot.EnemiesController.EnemyInfos.TryGetValue(fl.GetPlayer, out var info))
                        {
                            _bot.Memory.DeleteInfoAboutEnemy(fl.GetPlayer);
                        }
                        _bot.BotsGroup.AddEnemy(fl.GetPlayer, EBotEnemyCause.addPlayer);
                    });

                    if (_bot.EnemiesController.EnemyInfos.TryGetValue(_player.Player(), out var plinfo))
                    {
                        _bot.Memory.DeleteInfoAboutEnemy(_player.Player());
                    }

                    _bot.BotsGroup.CheckAndAddEnemy(_player.Player());
                    _bot.BotsGroup.Enemies.ExecuteForEach((key, value) =>
                    {
                        value.IsHaveSeen = key.ProfileId == _player.Player().ProfileId || BossPlayers.GetFollowers().Find(fl => fl.GetBot().ProfileId == key.ProfileId) != null;
                        _bot.Memory.AddEnemy(key, value, false);

                        if (key.ProfileId == _player.Player().ProfileId && _bot.EnemiesController.EnemyInfos.TryGetValue(_player.Player(), out var eninfo))
                        {
                            _bot.Memory.GoalEnemy = eninfo;
                            Modules.Logger.LogInfo("Make player the enemy");
                        }
                    });
                }

            }
            catch (Exception ex)
            {
                Modules.Logger.LogInfo("Error on dismiss for a follower: " + ex.Message);
                Modules.Logger.LogInfo(ex.StackTrace);
            }
            // @TODO : see what else can be reverted
        }
    }
}
