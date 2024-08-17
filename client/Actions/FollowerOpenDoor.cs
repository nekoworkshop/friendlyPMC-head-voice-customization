using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;
using friendlyPMC.Requests;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerOpenDoor : BaseNodeAbstractClass
    {

        private bool bool_0;
        private bool bool_1;

        private Door Door;

        public FollowerOpenDoor(BotOwner bot) : base(bot) {
        }

        public override void Update()
        {

            botOwner_0.DoorOpener.Update();

            if (bool_1) return;

            if (Door == null && botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.doorOpen) 
            {
                Components.Logger.LogInfo("Door SET");
                Door = (botOwner_0.BotRequestController.CurRequest as FollowerOpenDoorRequest).Door;
            }

            if (Door == null) {
                Components.Logger.LogInfo("No Door");
                ClearOpener();
                return;
            };

            if (Door.DoorState == EDoorState.Open)
            {
                ClearOpener();
                return;
            }

            if (!bool_0)
            {
                Vector3 position = Door.transform.position;

                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(position, out navMeshHit,2f, -1) && botOwner_0.GoToPoint(navMeshHit.position, false, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
                {
                    Components.Logger.LogInfo("Go to Door");

                    botOwner_0.GoToSomePointData.SetPoint(navMeshHit.position);
                    botOwner_0.GoToSomePointData.UpdateToGo(false);
                    botOwner_0.Steering.LookToMovingDirection();

                }
                else
                {
                    ClearOpener();
                }
                

                bool_0 = true;
                return;
            }

            if (!botOwner_0.GoToSomePointData.IsCome())
            {
                return;
            }

            if (!bool_1)
            {
                botOwner_0.StopMove();
                Components.Logger.LogInfo("Open Door");
                botOwner_0.DoorOpener.OnEndInteract += ClearOpener;
                botOwner_0.DoorOpener.Interact(Door, EInteractionType.Open);

                bool_1 = true;
            }

        }

        public void ClearOpener()
        {
            InteractableObjects.RemoveOpener(botOwner_0);
            Door = null;
            bool_0 = false;
            bool_1 = false;
            if(botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.doorOpen)
            {
                botOwner_0.BotRequestController.CurRequest.Complete();
            }
            botOwner_0.DoorOpener.OnEndInteract -= ClearOpener;

            Components.Logger.LogInfo("Clear Opener");
        }
    }
}
