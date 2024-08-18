using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class FollowerDoorLayer : GClass102
    {

        private float doorOpenTimer;
        public FollowerDoorLayer(BotOwner bot, int priority) : base(bot, priority) { 
        }

        public override bool ShallUseNow()
        {

            return InteractableObjects.IsOpener(botOwner_0);
        }

        public override string Name()
        {
            return "FBPDoorOpen";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            if (!InteractableObjects.IsOpener(botOwner_0))
            {
                InteractableObjects.RemoveTaker(botOwner_0);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "door.Error");
            }

            doorOpenTimer = Time.time + 7f;
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.doorOpen, "door.Open");
        }

        public override AICoreActionEndStruct EndDoorOpenRequest()
        {
            BotRequest curRequest = botOwner_0.BotRequestController.CurRequest;

            if (doorOpenTimer < Time.time)
            {

                if (InteractableObjects.IsOpener(botOwner_0)) InteractableObjects.RemoveOpener(botOwner_0);

                if (curRequest != null && curRequest.BotRequestType == BotRequestType.doorOpen)
                {
                    curRequest.Complete();
                }

                return new AICoreActionEndStruct("door.Timeout", true);
            }

            if (!InteractableObjects.IsOpener(botOwner_0))
            {

                if (curRequest != null && curRequest.BotRequestType == BotRequestType.doorOpen)
                {
                    curRequest.Complete();
                }

                return new AICoreActionEndStruct("door.None", true);
            }

            return aICoreActionEndStruct;
        }
    }
}
