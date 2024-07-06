using Aki.Common.Http;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using friendlyPMC.Actions;
using friendlyPMC.Requests;
using friendlyPMC.Modules;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;

namespace friendlyPMC.Components
{
    
    internal class FollowerReceiver : BotReceiver
    {

        private static Player interactivePlayer = null;

        private static float interactiveTime = 0f;
        
        private static float closestTime = 0f;

        private static BotOwner closestPlayer = null;

        private static readonly float maxGestusDistance = 18f;
        public FollowerReceiver(BotOwner owner) : base(owner)
        {
            Receivers.AddReceiver(owner.ProfileId, this);
        }


        private static Player GetInteractivePlayer(Vector3 requestPosition, Vector3 requestDirection)
        {

            if(interactiveTime > Time.time) return interactivePlayer;

            Ray ray = new Ray(requestPosition, requestDirection.normalized * maxGestusDistance);

            RaycastHit hit;
            Player player = GameWorld.FindInteractablePlayer(ray, out hit);

            interactivePlayer = player;

            interactiveTime = Time.time + 1f;

            return interactivePlayer;
        }

        private static BotOwner GetClosetBot(BotOwner bot, IPlayer requester, out FollowerGoCheck request)
        {

            if (closestTime > Time.time)
            {
                request = new FollowerGoCheck(closestPlayer);
                return closestPlayer;
            }

            BotOwner closest = null;
            float dist = Mathf.Infinity;
            pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
            Vector3 bossPos = boss.realPlayer.Transform.position;

            FollowerGoCheck gclass = new FollowerGoCheck(requester);

            boss.Followers.ForEach(fl =>
            {
                if (gclass.CanRequest(bot))
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
            request = gclass;

            return closestPlayer;
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

            bool isBossCommunicating = requester != null && BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.BossToFollow.IsMe(requester);

            return isBossCommunicating;
        }

        public bool IsAllyRequester(IPlayer requester)
        {
            return IsBossRequester(requester) || BossPlayers.Instance.IsFollower(botOwner_0) && requester != null && botOwner_0.BotsGroup.IsAlly(requester);
        }

        public virtual void GestusShown(GClass454 data)
        {

            EGesture gesture = data.Gesture;
            bool isBossCommunicating = IsBossRequester(data.Player);

            float gestusDistance = (botOwner_0.GetPlayer.Transform.position - data.Player.Transform.position).magnitude;

            bool shouldDefault = !BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(data.Player.ProfileId);

            List<EGesture> bossNoGesture = new List<EGesture>
            {
            };
            List<EGesture> bossBusyNoGesture = new List<EGesture>
            {
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

            // AI Boss followers will not take several gestures
            if (isBossCommunicating && isFollowerBoss)
            {
                if (gestusDistance < maxGestusDistance)
                {
                    if (bossNoGesture.Contains(gesture))
                    {

                        if (!botOwner_0.Memory.HaveEnemy) 
                        { 
                            botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                            botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
                        }
                        return;
                    }

                    if(botOwner_0.Memory.HaveEnemy && bossBusyNoGesture.Contains(gesture))
                    {
                        return;
                    }

                    
                }
            }
            // on gesture "stop" nearby bots will hold position
            if (gesture == EGesture.Stop)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {
                        botOwner_0.BotsGroup.RequestsController.TryActivateWait(data.Player, botOwner_0);
                    }
                } else if(shouldDefault)
                {
                    base.method_6(data);
                }
            }
            // on gesture "come here" only the bot that the player is looking at will come to the player
            else if(gesture == EGesture.ComeToMe)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {
                        Components.Logger.LogInfo("Come To me was made");
                        Player playerLook = GetInteractivePlayer(data.Player.Transform.position, data.Player.LookDirection);
                        
                        if(playerLook == null) Components.Logger.LogInfo("playerLook is null");

                        if (playerLook != null && playerLook.ProfileId == botOwner_0.ProfileId)
                        {
                            FollowerGoCheck gclass = new FollowerGoCheck(data.Player, BotRequestType.followMe);
                            if (
                                gclass.CanRequest(botOwner_0) &&
                                botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass)
                            )
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            }
                        }
                    }
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            // on gesture "go there", if bot has enemy, do a push, else the closest bot to the user will move forward 
            else if(gesture == EGesture.ThatDirection)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {

                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(data.Player.ProfileId);
                        
                        if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                        {
                            (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();
                            // if has enemy, on "That direction" rush the enemy
                            if (botOwner_0.Memory.HaveEnemy)
                            {
                                FollowerRushEnemy gclass = new FollowerRushEnemy(botOwner_0, alivePlayerByProfileID,BotRequestType.attackClose);

                                if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                {
                                    gclass.AddPossibleExecutors(botOwner_0);
                                    gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                                }

                            }
                            // else move somewhere in front of the player
                            else
                            {
                                FollowerGoCheck gclass;
                                BotOwner closest = GetClosetBot(botOwner_0,data.Player, out gclass);

                                // - the closest bot shall move
                                if (closest != null && gclass != null && botOwner_0 && botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                {
                                    gclass.AddPossibleExecutors(botOwner_0);
                                    gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
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

            bool shouldDefault = !BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(requester.ProfileId);

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

            bool isFollowerBoss = false;
            foreach (WildSpawnType role in Utils.Utils.BossFollowersRoles)
            {
                if(botOwner_0.IsRole(role))
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
                
                if(botOwner_0.Memory.HaveEnemy)
                {
                    botOwner_0.Memory.DeleteInfoAboutEnemy(botOwner_0.Memory.GoalEnemy.Person);
                }
                // force current layer to trigger end decision
                AccessTools.Field(typeof(BaseLogicLayerClass), "bool_1").SetValue(botOwner_0.Brain.BaseBrain.CurLayerInfo,true);
                // try to get bot unstuck in item taker logic
                InteractableObjects.RemoveTaker(botOwner_0);

                return;
            }


            // AI Boss followers and AI followers of AI Bosses will not take several commands
            if (isBossCommunicating && isFollowerBoss)
            {
                if(bossIgnore.Contains(info.phrase))
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


            if(isAllyRequesting)
            {
                // on supression, switch enemy priority
                if (info.phrase == EPhraseTrigger.Suppress)
                {

                    if(isBossCommunicating)
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
                    } else if(botOwner_0.Memory.HaveEnemy)
                    {
                        botOwner_0.BotsGroup.RequestsController.TryAskSuppressionRequest(requester, botOwner_0.Memory.GoalEnemy);
                    }

                    
                    return;
                }
            }

            if (isBossCommunicating)
            {

                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                // on cover me, make bot follow boss near
                if (info.phrase == EPhraseTrigger.CoverMe)
                {
                    FollowerPatrolInstances.SetNearPatrol(botOwner_0);
                }
                // on get back make bot follow boss at a distance
                else if(info.phrase == EPhraseTrigger.GetBack)
                {
                    FollowerPatrolInstances.SetFarPatrol(botOwner_0);

                }
                // on regroup all shall come near the boss
                else if(info.phrase == EPhraseTrigger.Regroup || (isFollowerBoss && info.phrase == EPhraseTrigger.FollowMe && !botOwner_0.Memory.HaveEnemy))
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Components.Logger.LogInfo("Boss Said to regroup");

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
                else if(info.phrase == EPhraseTrigger.FollowMe)
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

                        if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true))
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
                else if(info.phrase == EPhraseTrigger.Silence)
                {
                    if(isClose)
                    {
                        botOwner_0.BotTalk.SetSilence(60f);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
                }
                // attack close
                else if (info.phrase == EPhraseTrigger.Fire)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic("push");

                    if(isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
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
                // hold position
                else if (info.phrase == EPhraseTrigger.HoldPosition)
                {

                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic("defend");

                    if (isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }

                }
                // temporary hold position
                else if (info.phrase == EPhraseTrigger.Stop)
                {
                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                    FollowerHold holdit = new FollowerHold(alivePlayerByProfileID);

                    if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(holdit))
                    {
                        holdit.AddPossibleExecutors(botOwner_0);
                        holdit.SetGroup(botOwner_0.BotsGroup.RequestsController);
                    }

                }
                // reset boss tactic
                else if(info.phrase == EPhraseTrigger.Gogogo)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic(null);

                    if (isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
                }
                // scan for enemies in front
                else if(info.phrase == EPhraseTrigger.OnRepeatedContact)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    FollowerEnemyScan.ScanDirection(botOwner_0, info.PlayerRequester, boss.realPlayer);
                }
                // open door request
                else if (info.phrase == EPhraseTrigger.OpenDoor && !botOwner_0.Memory.HaveEnemy)
                {
                    Door door = InteractableObjects.GetCurDoor();
                    if (door != null)
                    {
                        
                        InteractableObjects.SetCurDoor(null);

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
                            botOwner_0.BotsGroup.RequestsController.TryActivateOpenDoorRequest(requester, door, null);
                        }
                    }
                }
                // loot item
                else if (info.phrase == EPhraseTrigger.LootGeneric || info.phrase == EPhraseTrigger.LootWeapon)
                {
                    if(!notBusy)
                    {
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
                        return;
                    }

                    LootItem  item = InteractableObjects.GetCurLootItem();
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
                        } catch 
                        { 
                            closest = null;
                        }

                        if (closest != null && closest.ProfileId == botOwner_0.ProfileId)
                        {
                            Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                            if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                            {
                                FollowerTakeLootRequest gclass = new FollowerTakeLootRequest(requester);

                                if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                                {
                                    gclass.AddPossibleExecutors(botOwner_0);
                                    gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                                }
                            }
                        }
                    }
                }
                else if(info.phrase == EPhraseTrigger.CheckHim || info.phrase == EPhraseTrigger.LootBody)
                {
                    if (!notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                        return;
                    }
                   
                    Corpse item = InteractableObjects.GetCurCorpse();
                    if (item != null)
                    {
                        float dist = Mathf.Infinity;
                        BotOwner closest = null;
                        boss.Followers.ForEach(fl =>
                        {
                            bool isAiBoss = false;
                            foreach (WildSpawnType role in Utils.Utils.BossFollowersRoles)
                            {
                                if (botOwner_0.IsRole(role))
                                {
                                    isAiBoss = true;
                                    break;
                                }
                            }
                            
                            if (isAiBoss) return;

                            Vector3 pos = fl.GetPlayer.Transform.position;
                            float fldist = (item.transform.position - pos).sqrMagnitude;
                            //fl.HealthController.
                            if (fldist < dist)
                            {
                                closest = fl;
                                dist = fldist;
                            }

                        });
                        if (closest != null && closest.ProfileId == botOwner_0.ProfileId)
                        {
                            InteractableObjects.SetTaker(botOwner_0);
                        }
                    }
                }
                // on dismiss remove the bot from being a follower
                else if (info.phrase == EPhraseTrigger.OnYourOwn)
                {
                    BotFollowerPlayer follower = BossPlayers.Instance.GetBossFollowers(boss.Player().ProfileId).Find((BotFollowerPlayer fl) =>
                    {
                        return fl.IsBot(botOwner_0);
                    });

                    if (follower != null)
                    {
                        BossPlayers.RemoveFollower(botOwner_0, boss, true);
                    }

                }

            } else if(shouldDefault)
            {
                base.method_0(info);
            }
        }

    }
}
