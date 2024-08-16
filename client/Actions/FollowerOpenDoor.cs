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
        private float float_0;

        private Door Door;
        public FollowerOpenDoorRequest GClass509_0
        {
            get
            {
                return botOwner_0.BotRequestController.CurRequest as FollowerOpenDoorRequest;
            }
        }

        public FollowerOpenDoor(BotOwner bot) : base(bot) {
        }

        public override void Update()
        {

            botOwner_0.DoorOpener.Update();

            if (Door == null) Door = InteractableObjects.GetCurDoor();

            if (Door == null) {
                ClearOpener();
                return;
            };

            if (float_0 < Time.time)
            {
                method_5();
            }
            
            botOwner_0.SetPose(1f);
            botOwner_0.SetTargetMoveSpeed(0.6f);
            botOwner_0.LookData.SetLookPointByHearing(null);
            botOwner_0.Sprint(false, true);

            if (!bool_0)
            {
                
                Components.Logger.LogInfo("Go to Door");
                Vector3 position = Door.transform.position;
                if(botOwner_0.GoToPoint(position, true, -1f, false, false, true, false) != NavMeshPathStatus.PathComplete)
                {
                    ClearOpener();
                    return;
                }
                bool_0 = true;
            }

            if (!botOwner_0.Mover.IsComeTo(this.botOwner_0.Settings.FileSettings.Move.REACH_DIST, false))
            {
                
                if (Door.DoorState == EDoorState.Open)
                {
                    ClearOpener();
                    return;
                }

                return;
            }

            if (!bool_1)
            {
                Components.Logger.LogInfo("Open Door");
                botOwner_0.StopMove();
                botOwner_0.DoorOpener.Interact(Door, EInteractionType.Open);

                ClearOpener();

                bool_1 = true;
            } else
            {
                if (Door.DoorState == EDoorState.Open)
                {
                    ClearOpener();
                    return;
                }
            }
        }
        public void method_5()
        {
            this.float_0 = Time.time + this.botOwner_0.Settings.FileSettings.Move.UPDATE_TIME_RECAL_WAY;
            this.bool_0 = false;
        }

        public void ClearOpener()
        {
            InteractableObjects.RemoveOpener();
            Door = null;
        }
    }
}
