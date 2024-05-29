using Comfort.Common;
using EFT;
using friendlyPMC.Modules;
using System;
using UnityEngine;

namespace friendlyPMC.Components
{
    
    internal class FollowerReceiver : BotReceiver
    {
        public FollowerReceiver(BotOwner owner) : base(owner)
        {
            Receivers.AddReciever(owner.ProfileId, this);
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

            Receivers.RemoveReciever(this);
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
                        !botOwner_0.BotFollower.HaveBoss || !BossPlayers.Instance.IsBoss(botOwner_0.BotFollower.BossToFollow.Player().ProfileId))
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
            base.method_0(info);
        }

    }
}
