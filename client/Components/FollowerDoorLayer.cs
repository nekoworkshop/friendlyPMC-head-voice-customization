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
    /**
     * Better handler for door opening for our followers to prevent them from getting stuck.
     */
    internal class FollowerDoorLayer : GClass120
    {

        private float doorOpenTimer;
        public FollowerDoorLayer(BotOwner bot, int priority) : base(bot, priority) { 
        }

        public override bool ShallUseNow()
        {
            var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;

            if (brain != null && brain.UnderFire) return false;

            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork || botOwner_0.Medecine.Using) return false;

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
