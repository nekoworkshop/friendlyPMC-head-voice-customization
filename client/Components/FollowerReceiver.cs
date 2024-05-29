using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using UnityEngine;

namespace friendlyPMC.Components
{
    
    internal class FollowerReceiver : BotReceiver
    {
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
            if (gesture == EGesture.Stop)
            {
                if (BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.BossToFollow.IsMe(data.Player))
                {
                    botOwner_0.BotsGroup.RequestsController.TryAskHoldRequest(data.Player, botOwner_0);
                } else if(
                    !BossPlayers.Instance.IsFollower(botOwner_0) && 
                    (
                        !botOwner_0.BotFollower.HaveBoss || !BossPlayers.Instance.IsBoss(botOwner_0.BotFollower.BossToFollow.Player().ProfileId)
                    )
                )
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
            // on cover me, get closer to the boss
            if (info.phrase == EPhraseTrigger.CoverMe)
            {
                IPlayer requester = info.PlayerRequester;
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                if (requester != null && boss != null && BossPlayers.Instance.IsFollower(botOwner_0, boss))
                {
                    boss.bossGroup.RequestsController.TryAskFollowMeRequest(requester, botOwner_0);
                    return;
                }
            }
            // on supression, switch enemy priority
            else if (info.phrase == EPhraseTrigger.Suppress)
            {
                IPlayer requester = info.PlayerRequester;
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                if (requester != null && boss != null && BossPlayers.Instance.IsFollower(botOwner_0, boss) && (botOwner_0.GetPlayer.Transform.position - requester.Transform.position).sqrMagnitude < 15f)
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

                        if (newEnemy != null && (botOwner_0.GetPlayer.Transform.position - enemyInfo.Person.Transform.position).sqrMagnitude > 30f)
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
                // on dismiss remove the bot from being a follower
            } else if (info.phrase == EPhraseTrigger.OnYourOwn)
            {
                IPlayer requester = info.PlayerRequester;
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                if (BossPlayers.Instance.IsBoss(requester.ProfileId))
                {
                    if (BossPlayers.Instance.IsFollower(botOwner_0, boss) && (botOwner_0.GetPlayer.Transform.position - boss.Position).sqrMagnitude < 10f)
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

                    return;
                }
            } else if (info.phrase == EPhraseTrigger.CheckHim || info.phrase == EPhraseTrigger.LootBody)
            {
                IPlayer requester = info.PlayerRequester;
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(requester.ProfileId);
                if (BossPlayers.Instance.IsBoss(requester.ProfileId))
                {
                    if (BossPlayers.Instance.IsFollower(botOwner_0, boss) && (botOwner_0.GetPlayer.Transform.position - boss.Position).sqrMagnitude < 12f)
                    {
                        //@TODO - have bot loot the body
                    }

                        return;
                }

            } else if(info.phrase == EPhraseTrigger.OpenDoor)
            { 
            } else
            {
                Logger.LogInfo("Phrase was " + info.phrase.ToString());
            }
            base.method_0(info);
        }

    }
}
