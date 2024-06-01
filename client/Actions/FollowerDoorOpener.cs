using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class GClass168_0 : BaseNodeClass
    {

        private bool bool_0;
        private float float_0;

        public GClass168_0(BotOwner bot, GClass509 doorClass) : base(bot) {
            GClass509_0 = doorClass;
        }

        private GClass509 GClass509_0;

        public override void Update()
        {
            base.botOwner_0.DoorOpener.Update();

            if (this.float_0 < Time.time)
            {
                this.method_5();
            }
            this.botOwner_0.SetPose(1f);
            this.botOwner_0.SetTargetMoveSpeed(0.6f);
            this.botOwner_0.LookData.SetLookPointByHearing(null);
            this.botOwner_0.Sprint(false, true);
            if (!this.bool_0)
            {
                Vector3 position = this.GClass509_0.Door.transform.position;
                this.botOwner_0.GoToPoint(position, true, -1f, false, true, true, false);
                this.bool_0 = true;
            }
            if (!this.botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                return;
            }
            this.method_4();
        }

        public void method_4()
        {
            this.botOwner_0.StopMove();
        }

        public void method_5()
        {
            this.float_0 = Time.time + 1f;
            this.bool_0 = false;
        }
    }
    internal class FollowerDoorOpener : BaseNodeClass
    {
        public FollowerDoorOpener(BotOwner bot, GClass509 doorClass) : base(bot) {

            GClass509_0 = doorClass;
            gclass168_0 = new GClass168_0(bot,doorClass);
        }
        
        private GClass509 GClass509_0;

        public override void Update()
        {
            if(GClass509_0 == null)
            {
                return;
            }

            if ((botOwner_0.Position - GClass509_0.Door.transform.position).magnitude < 0.5f)
            {
                botOwner_0.DoorOpener.Interact(GClass509_0.Door, EInteractionType.Open);

                return;
            }

            gclass168_0.Update();
        }


        private GClass168_0 gclass168_0;
    }
}
