using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerTakeLoot : BaseNodeClass
    {
        private BotFollowerPlayer _follower;

        private bool bool_0 = false;

        private bool bool_1 = false;    

        public FollowerTakeLoot(BotOwner bot) : base(bot)
        {
            _follower = BossPlayers.Instance.GetFollower(bot);
        }

        public override void Update()
        {
            if (_follower == null || _follower.LootingBrain == null) return;

            if (!bool_0)
            {

                Vector3 dest = Vector3.zero;
                bool destset = false;
                
                if(_follower.LootingBrain.ActiveCorpse != null || _follower.LootingBrain.ActiveItem != null)
                {
                    destset = true;
                }

                if (_follower.LootingBrain.ActiveItem != null) dest = _follower.LootingBrain.ActiveItem.transform.position;
                else if (_follower.LootingBrain.ActiveCorpse != null) dest = _follower.LootingBrain.ActiveCorpse.Transform.position;

                if(!destset)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    InteractableObjects.ClearTaker();
                    return;
                }

                NavMeshPathStatus pathStatus = botOwner_0.GoToPoint(dest, true, -1f, false, true, true, false);

                if (pathStatus != NavMeshPathStatus.PathComplete)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    InteractableObjects.ClearTaker();
                    return;
                }

                botOwner_0.SetPose(1f);
                botOwner_0.SetTargetMoveSpeed(1f);
                botOwner_0.Steering.LookToMovingDirection();

                bool_0 = true;
            }

            
            if (!botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                return;
            }

            if (!bool_1) {
                botOwner_0.StopMove();
                InteractableObjects.ClearTaker();
                _follower.LootingBrain.StartLooting();
                bool_1 = true;
            }
        }
    }
}
