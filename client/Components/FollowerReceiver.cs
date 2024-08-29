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

namespace friendlyPMC.Components
{

    internal class FollowerReceiver : BotReceiver
    {

        private static float closestTime = 0f;

        private static BotOwner closestPlayer = null;

        private static readonly float maxGestusDistance = 15f;
        public FollowerReceiver(BotOwner owner) : base(owner)
        {
            Receivers.AddReceiver(owner.ProfileId, this);
        }


        private static bool IsRequesterLookingAt(BotOwner bot, Player requester)
        {
            return bot.LookSensor.CheckLookSimple(requester, bot.GetPlayer) && bot.IsEnemyLookingAtMe(requester);
        }


        private static bool IsClosestBotToBoss(BotOwner bot)
        {
            if (closestTime > Time.time)
            {
                if (closestPlayer == null) return false;
                return closestPlayer.ProfileId == bot.ProfileId;
            }

            closestTime = Time.time + 0.5f;

            BotOwner closest = null;
            float dist = Mathf.Infinity;

            var boss = bot.BotFollower.BossToFollow;
            
            if (boss == null) return false;

            Vector3 bossPos = bot.BotFollower.BossToFollow.Player().Transform.position;

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

            closestPlayer = closest;

            if (closestPlayer.ProfileId == bot.ProfileId)
            {
                return true;
            }

            return false;
        }

        private static bool IsClosestBotToCheck(BotOwner bot, IPlayer requester, out FollowerGoCheck request)
        {
            request = null;

            if (closestTime > Time.time)
            {
                if (closestPlayer == null) return false;

                if (closestPlayer.ProfileId == bot.ProfileId)
                {
                    request = new FollowerGoCheck(closestPlayer);
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

            FollowerGoCheck gclass = new FollowerGoCheck(requester);

            boss.Followers.ForEach(fl =>
            {
                if (fl != null && gclass.CanRequest(fl))
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
                request = gclass;
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
                EGesture.ComeToMe
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

                if (gestusDistance > maxGestusDistance)
                {
                    return;
                }

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
                    if (gestusDistance < maxGestusDistance)
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                        if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, false))
                        {
                            FollowerHold holdit = new FollowerHold(playerRequester);

                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(holdit))
                            {
                                holdit.AddPossibleExecutors(botOwner_0);
                                holdit.SetGroup(botOwner_0.BotsGroup.RequestsController);
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
            else if (gesture == EGesture.ComeToMe)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance && IsRequesterLookingAt(botOwner_0, playerRequester)) 
                    {
                        FollowerGoCheck gclass = new FollowerGoCheck(data.Player, BotRequestType.followMe);

                        if (botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint)
                        {
                            Player requester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(data.Player.ProfileId);
                            botOwner_0.BotRequestController.TryStopCurrent(requester, false);
                        }

                        if (
                            gclass.CanRequest(botOwner_0) &&
                            botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass)
                        )
                        {
                            gclass.AddPossibleExecutors(botOwner_0);
                            gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                            if (botOwner_0.BotRequestController.CurRequest != null)
                            {
                                gclass.DisposeOtherRequestsWhenTaken = false;
                                botOwner_0.BotRequestController.SetCurrentRequest(gclass);
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
            else if (gesture == EGesture.Good)
            {
                if (isBossCommunicating)
                {
                    if (
                        gestusDistance < maxGestusDistance &&
                        IsRequesterLookingAt(botOwner_0, playerRequester) && IsClosestBotToBoss(botOwner_0) && 
                        !botOwner_0.Memory.HaveEnemy
                        )
                    {
                        Utils.Utils.SetTimeout(() =>
                        {
                            if (botOwner_0.BotState == EBotState.Active) botOwner_0.Gesture.TryGestus(EGesture.Good,false);
                        }, 1000);
                    }
                    return;
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            // on gesture "go there", if bot has enemy, do a push, else the closest bot to the user will move forward 
            else if (gesture == EGesture.ThatDirection)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {

                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(data.Player.ProfileId);

                        if (botOwner_0.Memory.HaveEnemy && botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                        {

                            (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                            // if has enemy, on "That direction" rush the enemy
                            FollowerRushEnemy gclass = new FollowerRushEnemy(botOwner_0, alivePlayerByProfileID, BotRequestType.attackClose);
                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            }
                        }
                        // else move somewhere in front of the player
                        // - the closest bot shall move
                        else if (!botOwner_0.Memory.HaveEnemy && IsClosestBotToBoss(botOwner_0))
                        {
                            if (botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint)
                            {
                                Player requester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(data.Player.ProfileId);
                                botOwner_0.BotRequestController.TryStopCurrent(requester, false);
                            }

                            FollowerGoCheck gclass;
                            
                            if (IsClosestBotToCheck(botOwner_0, data.Player, out gclass) && botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                if (botOwner_0.BotRequestController.CurRequest != null)
                                {
                                    gclass.DisposeOtherRequestsWhenTaken = false;
                                    botOwner_0.BotRequestController.SetCurrentRequest(gclass);
                                }
                            }
                        }
                    }
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
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

            bool isClose = (botOwner_0.GetPlayer.Transform.position - requester.Transform.position).magnitude < 14f;
            bool notBusy = !botOwner_0.Memory.HaveEnemy;

            List<EPhraseTrigger> bossNoPhrase = new List<EPhraseTrigger>
            {
                EPhraseTrigger.OpenDoor,
                EPhraseTrigger.Gogogo,
            };
            List<EPhraseTrigger> bossBusyIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.Stop,
                EPhraseTrigger.FollowMe,
            };

            List<EPhraseTrigger> bossIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.Silence,
                EPhraseTrigger.Fire,
                EPhraseTrigger.GetBack,
                EPhraseTrigger.GoForward,
                EPhraseTrigger.CoverMe,
                EPhraseTrigger.CheckHim,
                EPhraseTrigger.LootBody
            };

            List<EPhraseTrigger> allyNoPhrase = new List<EPhraseTrigger>{
                EPhraseTrigger.Silence,
                EPhraseTrigger.Fire,
                EPhraseTrigger.GetBack,
                EPhraseTrigger.GoForward,
                EPhraseTrigger.CoverMe,
                EPhraseTrigger.Stop,
                EPhraseTrigger.Gogogo,
                EPhraseTrigger.OpenDoor,
                EPhraseTrigger.HoldPosition
                
            };

            List<EPhraseTrigger> allyBusyIgnore = new List<EPhraseTrigger>
            {
                EPhraseTrigger.Regroup,
                EPhraseTrigger.FollowMe
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


            // AI Boss followers and AI followers of AI Bosses will not take several commands
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

            // scavs tend not to listen to anything
            if (isAssisting && isBossCommunicating)
            {
                if (allyNoPhrase.Contains(info.phrase))
                {
                    if (notBusy && isClose)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                    }
                    return;
                }
                else if (!notBusy && allyBusyIgnore.Contains(info.phrase))
                {
                    return;
                }
            }

            if (isAllyRequesting)
            {
                // on supression, switch enemy priority
                if (info.phrase == EPhraseTrigger.Suppress)
                {

                    if (isBossCommunicating)
                    {
                        pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);

                        if (isClose)
                        {
                            EnemyInfo enemyInfo;
                            if (!botOwner_0.Memory.HaveEnemy)
                            {
                                boss.PrioritizeEnemy(botOwner_0, boss.ClosestEnemy());
                                enemyInfo = botOwner_0.Memory.GoalEnemy;

                            }
                            else
                            {
                                enemyInfo = botOwner_0.Memory.GoalEnemy;
                                BotOwner newEnemy = boss.ClosestEnemy();

                                if (newEnemy != null && (enemyInfo == null || (botOwner_0.GetPlayer.Transform.position - enemyInfo.Person.Transform.position).magnitude > 20f))
                                {
                                    BotSettingsClass botSettingsClass = new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(newEnemy.ProfileId), boss.bossGroup, EBotEnemyCause.callForHelp1);

                                    botOwner_0.Memory.AddEnemy(newEnemy, botSettingsClass, false);

                                    enemyInfo = botOwner_0.Memory.GoalEnemy;
                                }
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
            }

            if (isBossCommunicating)
            {

                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                // on cover me
                if (info.phrase == EPhraseTrigger.CoverMe)
                {
                    // - make bot follow boss near
                    FollowerPatrolInstances.SetNearPatrol(botOwner_0);
                    // - cover boss when under attack
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).needsProtection = true;

                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                    // - regroup to boss
                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true))
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
                        return;
                    }

                }
                // on get back 
                else if (info.phrase == EPhraseTrigger.GetBack)
                {
                    // - make bot follow boss at a distance
                    FollowerPatrolInstances.SetFarPatrol(botOwner_0);
                    // - do not cover boss when under attack
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).needsProtection = false;

                }
                // on regroup all shall come near the boss
                else if (info.phrase == EPhraseTrigger.Regroup || (isFollowerBoss && info.phrase == EPhraseTrigger.FollowMe && !botOwner_0.Memory.HaveEnemy))
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true))
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
                else if (info.phrase == EPhraseTrigger.FollowMe)
                {
                    FollowerGoCheck gclass = new FollowerGoCheck(requester, BotRequestType.followMe);
                    if (
                        gclass.CanRequest(botOwner_0) &&
                        botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass)
                    )
                    {
                        gclass.AddPossibleExecutors(botOwner_0);
                        gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                    }
                    else
                    {
                        botOwner_0.BotRequestController.TrySayNegative(requester, gclass.BotRequestType);
                    }
                }
                // on need help closest bot shall come near boss
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

                        if (!botOwner_0.Memory.HaveEnemy)
                        {
                            botOwner_0.BotsGroup.RequestsController.TryAskFollowMeRequest(requester, botOwner_0);
                            return;
                        }


                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                        if (
                            botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true)
                        )
                        {
                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            }
                        }
                    }
                }
                // tell the bots to be quiet for a minute
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
                // move closer to enemy
                else if (info.phrase == EPhraseTrigger.GoForward)
                {
                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();
                        // if has enemy, on "go forward" move in closer to the enemy
                        if (botOwner_0.Memory.HaveEnemy)
                        {
                            FollowerRushEnemy gclass = new FollowerRushEnemy(botOwner_0, alivePlayerByProfileID, BotRequestType.goToPoint);
                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
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
                            }
                        }
                    }
                }
                // switch to hold tactic and try hold position request 
                else if (info.phrase == EPhraseTrigger.HoldPosition)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic("Defend");

                    if(botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType != BotRequestType.hold)
                        botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);

                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                    botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                }
                // disabled
                else if (info.phrase == EPhraseTrigger.Stop)
                {
                    return;
                }
                // reset boss tactic
                else if (info.phrase == EPhraseTrigger.Gogogo)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                    botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false);
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic(null);

                    if (isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
                }
                // scan for enemies in front
                else if (info.phrase == EPhraseTrigger.OnRepeatedContact)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    FollowerEnemyScan.ScanDirection(botOwner_0, info.PlayerRequester, boss.realPlayer);
                    return;
                }
                // open door request
                else if (info.phrase == EPhraseTrigger.OpenDoor && !botOwner_0.Memory.HaveEnemy)
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

                    return;
                }
                // loot item
                else if (info.phrase == EPhraseTrigger.LootGeneric || info.phrase == EPhraseTrigger.LootWeapon)
                {
                    if (!notBusy && botOwner_0.Memory.GoalEnemy.HaveSeen && Time.time - botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime < 3f)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
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

                                if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                                {
                                    FollowerTakeLootRequest gclass = new FollowerTakeLootRequest(requester);

                                    if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                    {
                                        gclass.AddPossibleExecutors(botOwner_0);
                                        gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);

                                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);

                                        return;
                                    }
                                    else
                                    {
                                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
                                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
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

                    return;
                }
                else if (info.phrase == (EPhraseTrigger)CustomPhrases.TeamStatus)
                {
                    if (notBusy)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Hello, false);
                    }
                    return;
                }
                else if (info.phrase == EPhraseTrigger.ExitLocated)
                {
                    if (isClose && notBusy)
                    {
                        Utils.Utils.SetTimeout(() =>
                        {
                            if(botOwner_0.BotState == EBotState.Active)
                                botOwner_0.BotTalk.TrySay(EPhraseTrigger.GoodWork, true);
                        }, 1000);
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

                    return;
                }
                // on dismiss remove the bot from being a follower
                else if (info.phrase == EPhraseTrigger.OnYourOwn)
                {
                    BotFollowerPlayer follower = BossPlayers.GetFollowersByBoss(boss.Player().ProfileId).Find((BotFollowerPlayer fl) =>
                    {
                        return fl.IsBot(botOwner_0);
                    });

                    if (follower != null)
                    {
                        BossPlayers.RemoveFollower(botOwner_0, boss, true);
                    }

                }

            }
            else if (shouldDefault)
            {
                base.method_0(info);
            }
        }

    }
}
