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

        public virtual void GestusShown(GClass454 data)
        {

            EGesture gesture = data.Gesture;
            bool isBossCommunicating = BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.BossToFollow.IsMe(data.Player);
            float gestusDistance = (botOwner_0.GetPlayer.Transform.position - data.Player.Transform.position).magnitude;

            bool shouldDefault = !BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(data.Player.ProfileId);

            if (gesture == EGesture.Stop)
            {
                if (isBossCommunicating)
                {
                    if (gestusDistance < maxGestusDistance)
                    {
                        botOwner_0.BotsGroup.RequestsController.TryAskHoldRequest(data.Player, botOwner_0);
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

            bool isBossCommunicating = requester !=null && BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.BossToFollow.IsMe(requester);
            bool shouldDefault = requester == null &&!BossPlayers.Instance.IsFollower(botOwner_0) && !BossPlayers.Instance.IsBoss(requester.ProfileId);
            
            if(isBossCommunicating)
            {
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                // on cover me, whoever is not close enough to the boss, come to him
                if (info.phrase == EPhraseTrigger.CoverMe || info.phrase == EPhraseTrigger.NeedHelp)
                {
                    if ((botOwner_0.GetPlayer.Transform.position - requester.Transform.position).magnitude < 15f)
                    {
                        Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);
                        if (botOwner_0.BotRequestController.TryStopCurrent(alivePlayerByProfileID.GetPlayer, true))
                        {
                            var @class = new FollowerCoverMe(alivePlayerByProfileID.GetPlayer);
                            if (@class.CanRequest(alivePlayerByProfileID.GetPlayer))
                            {
                                @class.AddPossibleExecutors(botOwner_0);
                                alivePlayerByProfileID.AIData.AskRequests.TryAdd(@class, botOwner_0.BotsGroup.RequestsController);
                            }
                        }
                    }

                    return;
                }
                // on regroup or follow me, get closer to the boss
                else if (info.phrase == EPhraseTrigger.FollowMe || info.phrase == EPhraseTrigger.Regroup)
                {
                    if (botOwner_0.Memory.HaveEnemy && info.phrase == EPhraseTrigger.Regroup)
                    {
                        botOwner_0.BotRequestController.SetCurrentRequest(new FollowerRegroup(requester));

                    }
                    botOwner_0.BotsGroup.RequestsController.TryAskFollowMeRequest(requester, botOwner_0);
                    return;
                } // on supression, switch enemy priority
                else if (info.phrase == EPhraseTrigger.Suppress)
                {
                    if ((botOwner_0.GetPlayer.Transform.position - requester.Transform.position).magnitude < 15f)
                    {
                        EnemyInfo enemyInfo;
                        if (!botOwner_0.Memory.HaveEnemy)
                        {
                            boss.PrioritizeEnemy(botOwner_0);
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
                else if (info.phrase == EPhraseTrigger.GoForward)
                {
                    if (botOwner_0.Memory.HaveEnemy)
                        botOwner_0.BotsGroup.RequestsController.TryActivateGoToCheckRequest(info.PlayerRequester, botOwner_0);
                    else
                    {
                        //@TODO - need to tell the bot to go to where the boss is pointing
                    }

                } // loot dead body
                else if ((info.phrase == EPhraseTrigger.CheckHim || info.phrase == EPhraseTrigger.LootBody) && !botOwner_0.Memory.HaveEnemy)
                {
                    BotOwner closest = null;
                    float dist = 10f;
                    Corpse corpse = InteractableObjects.GetCurCorpse();
                    if (corpse != null)
                    {
                        boss.Followers.ForEach(fl =>
                        {
                            Vector3 pos = fl.GetPlayer.Transform.position;
                            float fldist = (corpse.transform.position - pos).magnitude;
                            //fl.HealthController.
                            if (fldist < dist)
                            {
                                closest = fl;
                                dist = fldist;
                            }

                        });
                        if (closest != null && closest == botOwner_0)
                        {
                        }
                    }
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
                            botOwner_0.BotsGroup.RequestsController.TryActivateOpenDoorRequest(requester, door, null);
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
