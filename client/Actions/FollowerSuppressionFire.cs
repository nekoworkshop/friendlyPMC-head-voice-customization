using EFT;
using friendlyPMC.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Actions
{
    // GClass224 enhancement
    internal class FollowerSuppressionFire : GClass224
    {

        private float float_0;
        private readonly GClass145 gclass145_0;

        private bool _init = false;
        private bool grSupport = false;

        private Vector3? grPosition = null;
        private Vector3? grTarget = null;
        private bool grAllowed = true;

        private bool bool_1 = false;
        public FollowerSuppressionFire(BotOwner bot) : base(bot)
        {
            gclass145_0 = new GClass145(bot);
        }

        protected virtual void Init()
        {
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate += OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose += OnAgentDispose;

            _init = true;
        }
        protected void OnAgentUpdate(AICoreActionResultStruct<BotLogicDecision> decision)
        {
            if (
                decision.Action != BotLogicDecision.suppressFire && decision.Reason != "suppressFireLauncher"
            )
            {
                grSupport = false;
                grPosition = null;
                grAllowed = true;
                bool_1 = false;
            }
        }

        protected void OnAgentDispose(object sender, EventArgs e)
        {
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate -= OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose -= OnAgentDispose;
            _init = false;
            grSupport = false;
            grPosition = null;
            grAllowed = true;
            bool_1 = false;
        }

        public override void Update()
        {
            if (!botOwner_0.WeaponManager.Selector.IsWeaponReady) return;

            // our system that handles doing grenade launcher supression fire
            if (grAllowed)
            {
                if (bool_1)
                    gclass145_0.Update();

                if (grSupport && grPosition.HasValue)
                {
                    botOwner_0.DoorOpener.Update();

                    if(bool_1 && botOwner_0.GoToSomePointData.IsCome())
                    {
                        botOwner_0.StopMove();
                        bool_1 = true;
                    }

                    return;
                }

                var lastDecision = botOwner_0.Brain.Agent.LastResult();

                if (lastDecision.Reason == "suppressFireLauncher")
                {
                    grSupport = true;
                }

                Vector3? point = botOwner_0.SuppressShoot.GetPoint();
                if (point.HasValue && grSupport)
                {
                    ShootPointClass shootPointClass = new ShootPointClass(point.Value + BotOwner.STAY_HEIGHT, 0.7f);

                    if (GClass301.CanShootToTarget(shootPointClass, this.botOwner_0.WeaponRoot.position, this.botOwner_0.LookSensor.Mask, false))
                    {
                        bool_1 = true;
                        botOwner_0.StopMove();
                        botOwner_0.Steering.LookToPoint(shootPointClass.Point);
                        return;
                    }

                    Vector3? firePosition = Utils.Covers.FindShootPosition(botOwner_0, 5f, 50f, position =>
                    {
                        if (GClass301.CanShootToTarget(shootPointClass, position + this.botOwner_0.WeaponRoot.position, this.botOwner_0.LookSensor.Mask, false))
                        {
                            return true;
                        }
                        return false;
                    },true);

                    if (firePosition.HasValue)
                    {
                        grPosition = firePosition;
                        grTarget = point;
                        botOwner_0.GoToSomePointData.SetPoint(grPosition.Value);
                        botOwner_0.Steering.LookToPoint(grTarget.Value);
                        return;
                    }
                }

            }
            // game's system suppression 
            grAllowed = false;
            base.Update();
        }
    }
}
