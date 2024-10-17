using Comfort.Common;
using EFT;
using EFT.Interactive;
using friendlyPMC.Actions;
using friendlyPMC.Requests;
using friendlyPMC.Modules;
using System;

using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using EFT.InventoryLogic;

namespace friendlyPMC.Components
{

    internal class FollowerReceiver : BotReceiver
    {

        private static float closestTime = 0f;

        private static BotOwner closestPlayer = null;

        private static Player lookedAtPlayer = null;
        private static float lookedAtTime = 0f;

        private static readonly float maxGestusDistance = 15f;
        public FollowerReceiver(BotOwner owner) : base(owner)
        {
            Receivers.AddReceiver(owner.ProfileId, this);
        }

        private static Player IsRequesterLookingAtSomeone(Player requester, float magnitude = 27f)
        {
            if(lookedAtPlayer != null && lookedAtTime > Time.time) return lookedAtPlayer;

            float shpere_FRIENDY_FIRE_SIZE = 0.4f;

            LayerMask playerMask = LayerMaskClass.PlayerMask;

            RaycastHit[] array = new RaycastHit[10];
            Ray ray = requester.InteractionRay;

            pitAIBossPlayer boss = BossPlayers.GetBoss(requester.ProfileId);

            lookedAtPlayer = null;
            try
            {
                if (Physics.SphereCastNonAlloc(ray, shpere_FRIENDY_FIRE_SIZE, array, magnitude, playerMask) > 0)
                {
                    foreach (RaycastHit raycastHit in array)
                    {
                        if (raycastHit.collider != null && raycastHit.collider.gameObject != null)
                        {
                            if (
                                GClass301.CanShootToTarget(new ShootPointClass(raycastHit.point,1),ray.origin,LayerMaskClass.HighPolyWithTerrainMask)
                            )
                            //if (!Physics.Linecast(ray.origin, raycastHit.point, GameWorld.LootMaskObstruction))
                            {
                                BotOwner bot = raycastHit.collider.gameObject.GetComponent<BotOwner>();
                                if (bot != null && BossPlayers.IsFollower(bot, boss))
                                {

                                    lookedAtPlayer = bot.GetPlayer;
                                    break;
                                }
                            }
                        }
                    }
                }
            } catch(Exception ex) {
                Modules.Logger.LogError(ex);
            }

            lookedAtTime = Time.time + 0.5f;
            return lookedAtPlayer;
        }

        private static bool IsRequesterLookingAt(BotOwner bot, Player requester, float distance = 27f)
        {
            Player at = IsRequesterLookingAtSomeone(requester,distance);
            return at != null && at.ProfileId == bot.ProfileId;
        }

        private static bool IsClosestBot(BotOwner bot, IPlayer requester)
        {

            if (closestTime > Time.time)
            {
                if (closestPlayer == null) return false;

                if (closestPlayer.ProfileId == bot.ProfileId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            BotOwner closest = null;
            float dist = Mathf.Infinity;
            pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);

            if (boss == null) return false;

            Vector3 bossPos = boss.realPlayer.Transform.position;

            boss.Followers.ForEach(fl =>
            {
                if (fl != null)
                {
                    Vector3 pos = fl.GetPlayer.Transform.position;
                    float fldist = (bossPos - pos).sqrMagnitude;
                    if (fldist < dist)
                    {
                        closest = fl;
                        dist = fldist;
                    }
                }

            });

            closestTime = Time.time + 0.5f;

            closestPlayer = closest;

            if (closestPlayer.ProfileId == bot.ProfileId)
            {
                return true;
            }

            return false;
        }

        public virtual void Initiate()
        {
            Singleton<BotEventHandler>.Instance.OnQETilt += base.method_4;
            Singleton<BotEventHandler>.Instance.OnGestusShow += GestusShown;
            Singleton<BotEventHandler>.Instance.OnPhraseSay += PhraseSaid;
            Singleton<BotEventHandler>.Instance.OnHardAimDelegate += base.method_3;
            EPhraseTrigger[] array = (EPhraseTrigger[])Enum.GetValues(typeof(EPhraseTrigger));
        }

        public virtual void Destroy()
        {
            Singleton<BotEventHandler>.Instance.OnQETilt -= base.method_4;
            Singleton<BotEventHandler>.Instance.OnGestusShow -= GestusShown;
            Singleton<BotEventHandler>.Instance.OnPhraseSay -= PhraseSaid;
            Singleton<BotEventHandler>.Instance.OnHardAimDelegate -= base.method_3;

            Receivers.RemoveReceiver(this);
        }

        public bool IsBossRequester(IPlayer requester)
        {

            bool isBossCommunicating = !botOwner_0.BotFollower.HaveBoss ? false : requester != null && botOwner_0.BotFollower.BossToFollow.IsMe(requester);

            return isBossCommunicating;
        }

        public bool IsAllyRequester(IPlayer requester)
        {
            return IsBossRequester(requester) || (requester != null && botOwner_0.BotsGroup.IsAlly(requester));
        }

        public virtual void GestusShown(GClass453 data)
        {

            EGesture gesture = data.Gesture;
            bool isBossCommunicating = IsBossRequester(data.Player);

            bool isAssisting = (botOwner_0.Brain.BaseBrain as FollowerBrain).currentTactic == "Assist";

            float gestusDistance = (botOwner_0.GetPlayer.Transform.position - data.Player.Transform.position).magnitude;

            bool shouldDefault = !BossPlayers.IsPlayerBoss(data.Player.ProfileId);

            bool notBusy = !botOwner_0.Memory.HaveEnemy;

            List<EGesture> bossNoGesture = new List<EGesture>
            {
            };
            List<EGesture> bossBusyIgnore = new List<EGesture>
            {
                EGesture.Stop,
                EGesture.ComeToMe,
                EGesture.ThatDirection
            };

            List<EGesture> allyNoGesture = new List<EGesture> {
                EGesture.ThatDirection
            };
            List<EGesture> allyBusyIgnore = new List<EGesture>
            {
                EGesture.ComeToMe,
                EGesture.Stop
            };

            bool isFollowerBoss = false;
            foreach (WildSpawnType role in Utils.Utils.BossFollowersRoles)
            {
                if (botOwner_0.IsRole(role))
                {
                    isFollowerBoss = true;
                    break;
                }
            }

            // AI Boss followers and scavs will not take several gestures
            if (isBossCommunicating)
            {

                if (isAssisting)
                {
                    if (allyNoGesture.Contains(gesture)) 
                    { 
                        if(notBusy) botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        return;

                    } 
                    else if (!notBusy && allyBusyIgnore.Contains(gesture))
                    {
                        return;
                    }
                }
                else if (isFollowerBoss)
                {
                    if (bossNoGesture.Contains(gesture))
                    {

                        if (notBusy)
                        {
                            botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        }
                        return;
                    }

                    if (!notBusy && bossBusyIgnore.Contains(gesture))
                    {
                        return;
                    }
                }
            }

            Player playerRequester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(data.Player.ProfileId);

            // on gesture "stop" nearby bots will hold position
            if (gesture == EGesture.Stop)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance <= maxGestusDistance)
                    {
                        
                        if(botOwner_0.Memory.HaveEnemy)
                        {
                            if(!botOwner_0.Memory.GoalEnemy.IsVisible) botOwner_0.BotTalk.Say(EPhraseTrigger.Negative, true, null);
                            return;
                        }

                        if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, false))
                        {
                            FollowerHold holdit = new FollowerHold(playerRequester);

                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(holdit))
                            {
                                holdit.AddPossibleExecutors(botOwner_0);
                                holdit.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                                botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                            }
                        }
                    }

                    return;
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            // on gesture "come here" only the bot that the player is looking at will come to the player
            // on gesture "go there", the closest bot to the user will move forward  
            else if (gesture == EGesture.ComeToMe || gesture == EGesture.ThatDirection)
            {
                bool goThere = gesture == EGesture.ThatDirection;

                if (isBossCommunicating)
                {
                    if (
                        (IsRequesterLookingAt(botOwner_0, playerRequester) && !goThere) ||
                        (goThere && gestusDistance <= maxGestusDistance && IsClosestBot(botOwner_0, playerRequester))
                    ) 
                    {
                        bool hadHold = botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.wait;

                        FollowerGoCheck gclass = new FollowerGoCheck(data.Player, goThere ? BotRequestType.goToPoint :  BotRequestType.followMe,hadHold);


                        if (
                            botOwner_0.BotRequestController.TryStopCurrent(playerRequester, false) &&
                            gclass.CanRequest(botOwner_0) &&
                            botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass)
                        )
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            if(gesture != EGesture.ThatDirection) botOwner_0.Gesture.TryGestus(EGesture.Good, false);

                        }
                    }
                    return;
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            else if (gesture == EGesture.Good)
            {
                if (isBossCommunicating)
                {
                    if (
                        gestusDistance <= maxGestusDistance &&
                        IsRequesterLookingAt(botOwner_0, playerRequester) && 
                        !botOwner_0.Memory.HaveEnemy
                        )
                    {
                        Utils.Utils.SetTimeout(() =>
                        {
                            if (botOwner_0.BotState == EBotState.Active) botOwner_0.Gesture.TryGestus(EGesture.Good,false);
                        }, 500);
                    }
                    return;
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            else if(gesture == (EGesture)CustomGestures.OverThere)
            {
                (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();
                if(!botOwner_0.Memory.HaveEnemy)
                {
                    botOwner_0.BotTalk.SetSilence(2f);
                }
                FollowerEnemyCheck.CheckBossReport(botOwner_0);
            }
            else
            {

                base.method_6(data);
            }
        }

        public virtual void PhraseSaid(BotEventHandler.GClass599 info)
        {
            IPlayer requester = info.PlayerRequester;

            bool isBossCommunicating = IsBossRequester(requester);

            bool isAllyRequesting = IsAllyRequester(requester);

            bool isAssisting = (botOwner_0.Brain.BaseBrain as FollowerBrain).currentTactic == "Assist";

            bool shouldDefault = !BossPlayers.IsPlayerBoss(requester.ProfileId);

            bool isClose = (botOwner_0.GetPlayer.Transform.position - requester.Transform.position).magnitude < 23f;
            bool notBusy = !botOwner_0.Memory.HaveEnemy;

            List<EPhraseTrigger> bossNoPhrase = new List<EPhraseTrigger>
            {
                EPhraseTrigger.OpenDoor,
                EPhraseTrigger.Gogogo,
                EPhraseTrigger.GoForward,
                EPhraseTrigger.Silence,
                EPhraseTrigger.Fire,
                EPhraseTrigger.GetBack,
                EPhraseTrigger.CoverMe,
                EPhraseTrigger.HoldPosition
            };
            List<EPhraseTrigger> bossBusyIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.Stop,
                EPhraseTrigger.FollowMe,
            };

            List<EPhraseTrigger> bossIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.OnYourOwn
            };

            List<EPhraseTrigger> allyNoPhrase = new List<EPhraseTrigger>{
                EPhraseTrigger.Silence,
                EPhraseTrigger.Fire,
                EPhraseTrigger.GetBack,
                EPhraseTrigger.GoForward,
                EPhraseTrigger.Gogogo,
                EPhraseTrigger.OpenDoor,
                EPhraseTrigger.HoldPosition
                
            };

            List<EPhraseTrigger> allyBusyIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.Stop,
                EPhraseTrigger.FollowMe
            };
            
            List<EPhraseTrigger> allyIgnore = new List<EPhraseTrigger> {
                EPhraseTrigger.OnYourOwn
            };

            bool isFollowerBoss = false;
            foreach (WildSpawnType role in Utils.Utils.BossFollowersRoles)
            {
                if (botOwner_0.IsRole(role))
                {
                    isFollowerBoss = true;
                    break;
                }
            }

            // on Attention reset bot request and enemy state
            if (info.phrase == EPhraseTrigger.Attention)
            {
                if (botOwner_0.BotRequestController.CurRequest != null)
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                // force current layer to trigger end decision
                AccessTools.Field(typeof(BaseLogicLayerAbstractClass), "bool_1").SetValue(botOwner_0.Brain.BaseBrain.CurLayerInfo, true);
                // try to get bot unstuck in item taker logic
                InteractableObjects.RemoveTaker(botOwner_0);
                // try to get bot unstuck in open door logic
                InteractableObjects.RemoveOpener(botOwner_0);
                // clear current enemy
                if (botOwner_0.Memory.HaveEnemy)
                {
                    botOwner_0.Memory.DeleteInfoAboutEnemy(botOwner_0.Memory.GoalEnemy.Person);
                    botOwner_0.Memory.GoalEnemy = null;
                }

                return;
            }


            // AI Boss followers and AI Bosses will not take several commands
            if (isBossCommunicating && isFollowerBoss)
            {
                if (bossIgnore.Contains(info.phrase))
                {
                    return;
                }

                if (bossNoPhrase.Contains(info.phrase))
                {

                    if (!botOwner_0.Memory.HaveEnemy)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
                    }
                    return;
                }

                if (botOwner_0.Memory.HaveEnemy && bossBusyIgnore.Contains(info.phrase))
                {
                    return;
                }
            }

            // Bots in "Assist" will not take several commands
            if (isAssisting && isBossCommunicating)
            {
                if (allyNoPhrase.Contains(info.phrase))
                {
                    if (notBusy && isClose)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                    }
                    return;
                }
                else if (!notBusy && allyBusyIgnore.Contains(info.phrase))
                {
                    return;
                } 

                if(allyIgnore.Contains(info.phrase))
                {
                    return;
                }
            }

            if (isAllyRequesting)
            {
                // on Supression, switch enemy priority
                if (info.phrase == EPhraseTrigger.Suppress)
                {

                    if (isBossCommunicating)
                    {
                        
                        pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);

                        bool isGrenadier = false;

                        GClass396 selector = botOwner_0.WeaponManager.Selector as GClass396;
                        if(
                            selector != null && 
                            selector.SecondPrimaryWeapon as Weapon != null && 
                            (selector.SecondPrimaryWeapon as Weapon).IsGrenadeLauncher &&
                            (botOwner_0.Brain.BaseBrain as FollowerBrain)?.defaultTactic == "Guard"
                        )
                        {
                                isGrenadier = true;
                        }

                        if (isClose)
                        {
                            // - grenadiers do not need to switch enemies
                            if(isGrenadier) 
                            {
                                Player playerRequester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                                if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, true))
                                {
                                    FollowerSuppress gclass = new FollowerSuppress(requester);

                                    if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                    {
                                        gclass.AddPossibleExecutors(botOwner_0);
                                        gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                                    }
                                    return;
                                }

                                return;
                            }
                            EnemyInfo enemyInfo;
                            if (!botOwner_0.Memory.HaveEnemy)
                            {
                                boss.PrioritizeEnemy(botOwner_0, boss.ClosestEnemy());
                                enemyInfo = botOwner_0.Memory.GoalEnemy;

                            }
                            else
                            {
                                enemyInfo = botOwner_0.Memory.GoalEnemy;
                            }

                            if (enemyInfo != null)
                                boss.bossGroup.RequestsController.TryAskSuppressionRequest(requester, enemyInfo);
                        }
                    }
                    else if (botOwner_0.Memory.HaveEnemy)
                    {
                        botOwner_0.BotsGroup.RequestsController.TryAskSuppressionRequest(requester, botOwner_0.Memory.GoalEnemy);
                    }


                    return;
                }
                // on Spreadout look for a random cover
                else if (info.phrase == EPhraseTrigger.Spreadout)
                {
                    Player ally = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(ally, true))
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                        FollowerTakeCover gclass = new FollowerTakeCover(ally);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            if (isClose && (notBusy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                            {
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, false);
                            }
                        }
                    }

                    return;
                }
            }

            if (isBossCommunicating)
            {

                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                Player playerRequester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                Player botLookedAt = IsRequesterLookingAtSomeone(playerRequester,37f);

                if (botLookedAt != null && botLookedAt.ProfileId == botOwner_0.ProfileId)
                {
                    isClose = true;
                }

                // on Cover Me follow close and try to cover player in fights
                if (info.phrase == EPhraseTrigger.CoverMe && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    // - make bot follow boss near
                    FollowerPatrolInstances.SetNearPatrol(botOwner_0);
                    // - reset bot tactic
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic(null);
                    // - cover boss when under attack
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).bossNeedsProtection = true;
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    // - regroup to boss
                    if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, true))
                    {
                        FollowerRegroup gclass = new FollowerRegroup(requester);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            if (isClose && (notBusy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                            {
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                            }
                        }
                        return;
                    }

                }
                // on Get Back follow at a distance
                else if (info.phrase == EPhraseTrigger.GetBack && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    FollowerPatrolInstances.SetFarPatrol(botOwner_0);

                    if(isClose) {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger,true);
                        botOwner_0.Gesture.TryGestus(EGesture.Good,true);
                    }

                }
                // on Regroup all shall come near the boss
                else if (info.phrase == EPhraseTrigger.Regroup)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                    {
                        FollowerRegroup gclass = new FollowerRegroup(requester);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            if (isClose && (notBusy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                            {
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                                botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                            }
                        }
                        else if (isClose && (notBusy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                        {
                            botOwner_0.BotRequestController.TrySayNegative(requester, gclass.BotRequestType);
                        }
                    }
                }
                // on Follow Me reset to follower patrol
                else if (info.phrase == EPhraseTrigger.FollowMe && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    if(botOwner_0.Memory.HaveEnemy)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, true);
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, false);

                        return;
                    }

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                    botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);
                    if(isClose) {
                        botOwner_0.Gesture.TryGestus(EGesture.Good,true);
                    }
                }
                // on Need Help closest bot shall come near boss
                else if (info.phrase == EPhraseTrigger.NeedHelp)
                {
                    BotOwner closest = null;
                    float dist = Mathf.Infinity;
                    pitAIBossPlayer pitBoss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                    Vector3 bossPos = boss.realPlayer.Transform.position;

                    FollowerRegroup gclass = new FollowerRegroup(requester);

                    pitBoss.Followers.ForEach(fl =>
                    {
                        if (gclass.CanRequest(botOwner_0))
                        {
                            Vector3 pos = fl.GetPlayer.Transform.position;
                            float fldist = (bossPos - pos).sqrMagnitude;
                            if (fldist < dist)
                            {
                                closest = fl;
                                dist = fldist;
                            }
                        }

                    });


                    if (closest != null && closest.ProfileId == botOwner_0.ProfileId)
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();


                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                        if (
                            botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true)
                        )
                        {
                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                if(isClose) botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger,true);
                            }
                        }
                    }
                }
                // one Silence be quiet for a minute
                else if (info.phrase == EPhraseTrigger.Silence)
                {
                    botOwner_0.BotTalk.SetSilence(120f);
                    if (isClose)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
                }
                // disabled
                else if (info.phrase == EPhraseTrigger.Fire)
                {
                    return;
                }
                // on Go Forward move closer to enemy
                else if (info.phrase == EPhraseTrigger.GoForward && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();
                        // if has enemy, on "go forward" move in closer to the enemy
                        if (botOwner_0.Memory.HaveEnemy)
                        {
                            FollowerRushEnemy gclass = new FollowerRushEnemy(botOwner_0, alivePlayerByProfileID);
                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                if(isClose) botOwner_0.BotTalk.TrySay(EPhraseTrigger.MumblePhrase, true);
                            }
                        }
                        // else move somewhere in front of the player
                        else
                        {
                            FollowerGoCheck gclass = new FollowerGoCheck(alivePlayerByProfileID);

                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                if(isClose) {
                                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, true);
                                    botOwner_0.Gesture.TryGestus(EGesture.Good,true);
                                }
                            }
                        }
                    }
                }
                // on Hold Position switch to hold tactic 
                else if (info.phrase == EPhraseTrigger.HoldPosition && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic("Defend");
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType != BotRequestType.wait)
                        botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);

                    if(isClose) {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                        botOwner_0.Gesture.TryGestus(EGesture.Good,true);
                    }
                }
                // on Stop hold in place
                else if (info.phrase == EPhraseTrigger.Stop && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    if(botOwner_0.Memory.HaveEnemy)
                    {
                        if(isClose && !botOwner_0.Memory.GoalEnemy.IsVisible) botOwner_0.BotTalk.Say(EPhraseTrigger.Negative, true, null);
                        return;
                    } 

                    if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, false))
                    {
                        FollowerHold holdit = new FollowerHold(playerRequester);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(holdit))
                        {
                            holdit.AddPossibleExecutors(botOwner_0);
                            holdit.SetGroup(botOwner_0.BotsGroup.RequestsController);

                            if(isClose) {
                                botOwner_0.Gesture.TryGestus(EGesture.Good, true);
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                            }
                        }
                    }
                }
                // on Go Go Go reset tactic
                else if (info.phrase == EPhraseTrigger.Gogogo && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic(null);
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                    if (botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType != BotRequestType.wait)
                        botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);

                    if (isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                    }
                }
                // on Contact scan for enemies in front
                else if (info.phrase == EPhraseTrigger.OnRepeatedContact)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();
                    FollowerEnemyCheck.CheckBossReport(botOwner_0); 
                }
                // open door request
                else if (info.phrase == EPhraseTrigger.OpenDoor && !botOwner_0.Memory.HaveEnemy && isClose)
                {
                    Door door = InteractableObjects.GetCurDoor();
                    if (door != null)
                    {
                        BotOwner closest = null;
                        float dist = 10f;
                        boss.Followers.ForEach(fl =>
                        {
                            Vector3 pos = fl.GetPlayer.Transform.position;
                            float fldist = (door.transform.position - pos).magnitude;
                            if (fldist < dist)
                            {
                                closest = fl;
                                dist = fldist;
                            }

                        });
                        // - the closest bot shall open the door
                        if (closest != null && closest == botOwner_0)
                        {
                            // -- cannot open locked doors
                            if(door.DoorState == EDoorState.Locked)
                            {
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                                botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                                return;
                            }

                            Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                            FollowerOpenDoorRequest gclass = new FollowerOpenDoorRequest(door, alivePlayerByProfileID);

                            if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                            {

                                if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                {
                                    gclass.AddPossibleExecutors(botOwner_0);
                                    gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                                }
                                else
                                {
                                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                                    botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                                    InteractableObjects.RemoveOpener(botOwner_0);
                                }
                            }
                        }
                    }

                }
                // loot item
                else if (info.phrase == EPhraseTrigger.LootGeneric || info.phrase == EPhraseTrigger.LootWeapon)
                {
                    if(!isClose) return;

                    if (!notBusy && botOwner_0.Memory.GoalEnemy.HaveSeen && Time.time - botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime < 3f)
                    {
                        
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, true);
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, false);
                        return;
                    }

                    LootItem item = InteractableObjects.GetCurLootItem();
                    if (item != null)
                    {
                        float dist = Mathf.Infinity;
                        BotOwner closest = null;
                        try
                        {
                            boss.Followers.ForEach(fl =>
                            {
                                Vector3 pos = fl.GetPlayer.Transform.position;
                                float fldist = (item.transform.position - pos).sqrMagnitude;
                                //fl.HealthController.
                                if (fldist < dist)
                                {
                                    closest = fl;
                                    dist = fldist;
                                }

                            });
                        }
                        catch
                        {
                            closest = null;
                        }

                        if (closest != null && closest.ProfileId == botOwner_0.ProfileId)
                        {
                            if (InteractableObjects.SetTaker(botOwner_0))
                            {
                                Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                                bool fromWait = botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.wait;

                                if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                                {
                                    FollowerTakeLootRequest gclass = new FollowerTakeLootRequest(requester, fromWait);

                                    if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                    {
                                        gclass.AddPossibleExecutors(botOwner_0);
                                        gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                        if(isClose) botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);

                                        return;
                                    }
                                    else
                                    {
                                        if(isClose) 
                                        {
                                            botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                                            botOwner_0.Gesture.TryGestus(EGesture.Bad, true);
                                        }
                                        InteractableObjects.RemoveTaker(botOwner_0);
                                    }
                                } 
                                else
                                {
                                    InteractableObjects.RemoveTaker(botOwner_0);
                                }
                            }
                        }
                    }
                }
                else if (info.phrase == (EPhraseTrigger)CustomPhrases.TeamStatus)
                {
                    if (notBusy && isClose)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Hello, false);
                    }
                }
                else if (info.phrase == EPhraseTrigger.ExitLocated)
                {
                    if (isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.GoodWork, true);
                    }

                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true))
                    {
                        FollowerRegroup gclass = new FollowerRegroup(requester);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                        }
                    }
                }
                // on On Your Own do not cover player when under attack
                else if (info.phrase == EPhraseTrigger.OnYourOwn && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).bossNeedsProtection = false;
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic(null);
                    FollowerPatrolInstances.SetFarPatrol(botOwner_0);

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType != BotRequestType.wait)
                        botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);

                    if (isClose) botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                } 
                else if (info.phrase == EPhraseTrigger.InTheFront)
                {
                    Player voicer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    FollowerBrain brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
                    if (brain != null) brain.FakeShot(voicer.MainParts[BodyPartType.head].Position + voicer.LookDirection * 20f);
                }
                else if (info.phrase == EPhraseTrigger.OnSix && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    Player voicer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    FollowerBrain brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
                    if (brain != null) brain.FakeShot(voicer.MainParts[BodyPartType.head].Position - voicer.LookDirection * 20f);
                }
                else if (info.phrase == EPhraseTrigger.LeftFlank && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    Player voicer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    FollowerBrain brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
                    if (brain != null) brain.FakeShot(voicer.MainParts[BodyPartType.head].Position + Quaternion.Euler(0, -90, 0) * voicer.LookDirection * 20f);
                }
                else if (info.phrase == EPhraseTrigger.RightFlank && (botLookedAt == null || botLookedAt.ProfileId == botOwner_0.ProfileId))
                {
                    Player voicer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    FollowerBrain brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
                    if (brain != null) brain.FakeShot(voicer.MainParts[BodyPartType.head].Position + Quaternion.Euler(0, 90, 0) * voicer.LookDirection * 20f);
                }

            }
            else if (shouldDefault)
            {
                base.method_0(info);
            }
        }

    }
}
