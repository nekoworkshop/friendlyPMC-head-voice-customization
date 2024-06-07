using Aki.Common.Http;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using friendlyPMC.Actions;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components
{
    
    internal class FollowerReceiver : BotReceiver
    {
        private readonly float maxGestusDistance = 20f;
        public FollowerReceiver(BotOwner owner) : base(owner)
        {
            Receivers.AddReceiver(owner.ProfileId, this);
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
            return BossPlayers.Instance.IsFollower(botOwner_0) && requester != null && botOwner_0.BotsGroup.IsAlly(requester);
        }

        public virtual void GestusShown(GClass454 data)
        {

            EGesture gesture = data.Gesture;
            bool isBossCommunicating = IsBossRequester(data.Player);

            float gestusDistance = (botOwner_0.GetPlayer.Transform.position - data.Player.Transform.position).magnitude;

            bool shouldDefault = !BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(data.Player.ProfileId);

            if(isBossCommunicating)
            {
                Logger.LogInfo("Boss gesture is " + gesture.ToString());
            }

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
            else if(gesture == EGesture.ComeToMe)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {
                        botOwner_0.BotsGroup.RequestsController.TryAskFollowMeRequest(data.Player, botOwner_0);
                    }
                }
                else if (shouldDefault)
                {
                    base.method_6(data);
                }
            }
            else if(gesture == EGesture.ThatDirection)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {
                        if( botOwner_0.Memory.HaveEnemy)
                            botOwner_0.BotsGroup.RequestsController.TryActivateGoToCheckRequest(data.Player, botOwner_0);
                        else
                        {
                            //@TODO - need to tell the bot to go to where the boss is pointing
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
            bool shouldDefault = !BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(requester.ProfileId);

            bool isClose = (botOwner_0.GetPlayer.Transform.position - requester.Transform.position).magnitude < 14f;
            bool notBusy = !botOwner_0.Memory.HaveEnemy;


            if (isBossCommunicating)
            {
                Logger.LogInfo("Boss said " + info.phrase.ToString());

                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                // on cover me, make bot follow boss near
                if (info.phrase == EPhraseTrigger.CoverMe)
                {
                    FollowerPatrolInstances.SetNearPatrol(botOwner_0);

                    return;
                }
                // on get back make bot follow boss at a distance
                else if(info.phrase == EPhraseTrigger.GetBack)
                {
                    FollowerPatrolInstances.SetFarPatrol(botOwner_0);
                    return;
                }
                // on need help get the closest bot to come near the boss
                else if(info.phrase == EPhraseTrigger.NeedHelp)
                {
                    if (isClose)
                    {
                        (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                        if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, true))
                        {
                            FollowerRegroup gclass = new FollowerRegroup(requester);

                           
                            if(botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            }
                        }
                    }
                }
                // on regroup all get closer to the boss
                else if (info.phrase == EPhraseTrigger.Regroup)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    if (botOwner_0.Memory.HaveEnemy)
                    {
                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                        if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID, false))
                        {
                            FollowerRegroup gclass = new FollowerRegroup(requester);

                            if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                            {
                                gclass.AddPossibleExecutors(botOwner_0);
                                gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            }
                        }

                    }
                    else
                        botOwner_0.BotsGroup.RequestsController.TryAskFollowMeRequest(requester, botOwner_0);
                    
                    return;

                } // on supression, switch enemy priority
                else if (info.phrase == EPhraseTrigger.Suppress)
                {
                    if (isClose)
                    {
                        EnemyInfo enemyInfo;
                        if (!botOwner_0.Memory.HaveEnemy)
                        {
                            boss.PrioritizeEnemy(boss.ClosestEnemy(), botOwner_0);
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

                        return;
                    }

                } // attack close
                else if (info.phrase == EPhraseTrigger.Fire)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).SetBossTactic("push");

                    if(isClose && notBusy)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                        botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    }
                }
                // temporary  attack close
                else if (info.phrase == EPhraseTrigger.GoForward)
                {
                    (botOwner_0.Brain.BaseBrain as FollowerBrain).BossOrdersChanged();

                    Vector3 enemyLastPosition = botOwner_0.Position;
                    if (botOwner_0.Memory.HaveEnemy)
                    {

                        enemyLastPosition = botOwner_0.Memory.LastEnemy.EnemyLastPosition;

                    }

                    Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    FollowerRushEnemy gclass = new FollowerRushEnemy(alivePlayerByProfileID, enemyLastPosition, null, null, BotRequestType.goToPoint);
                  
                    if(botOwner_0.BotsGroup.RequestsController.TryAddRequest(gclass))
                    {
                        gclass.AddPossibleExecutors(botOwner_0);
                        gclass.SetGroup(botOwner_0.BotsGroup.RequestsController);
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
                    if (isClose)
                    {
                        if(!notBusy) botOwner_0.BotsGroup.RequestsController.TryAskHoldRequest(requester, botOwner_0);
                        else botOwner_0.BotsGroup.RequestsController.TryActivateWait(requester, botOwner_0);
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
                else if ((info.phrase == EPhraseTrigger.LootGeneric || info.phrase == EPhraseTrigger.LootWeapon) && !botOwner_0.Memory.HaveEnemy)
                {

                    LootItem  item = InteractableObjects.GetCurLootItem();
                    if (item != null)
                    {
                        float dist = Mathf.Infinity;
                        BotOwner closest = null;
                        boss.Followers.ForEach(fl =>
                        {
                            Vector3 pos = fl.GetPlayer.Transform.position;
                            float fldist = (item.transform.position - pos).magnitude;
                            //fl.HealthController.
                            if (fldist < dist)
                            {
                                closest = fl;
                                dist = fldist;
                            }

                        });
                        if (closest != null && closest.ProfileId == botOwner_0.ProfileId)
                        {
                            InteractableObjects.SetTaker(botOwner_0, item);
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
                        BossPlayers.Instance.RemoveFollower(botOwner_0, boss, true);
                    }

                }

            } else if(shouldDefault)
            {
                base.method_0(info);
            }
        }

    }
}
