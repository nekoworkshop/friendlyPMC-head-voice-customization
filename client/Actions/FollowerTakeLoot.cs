using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;

using UnityEngine;
using UnityEngine.AI;

using Cysharp.Threading.Tasks;

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
                else if (_follower.LootingBrain.ActiveCorpse != null) dest = _follower.LootingBrain.ActiveCorpse.gameObject.transform.position;

                if(!destset)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    return;
                }

                // try get closer to the loot item/body
                NavMeshPathStatus pathStatus = botOwner_0.GoToPoint(dest, true, -1f, false, false, true, false);

                if (pathStatus != NavMeshPathStatus.PathComplete)
                {
                    Components.Logger.LogInfo("Path not found for loot taker");
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    return;
                }

                _follower.LootingBrain.Destination = dest;
                _follower.LootingBrain.LootObjectPosition = dest;
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
                Components.Logger.LogInfo("Start Looting");
                botOwner_0.SetPose(0f);
                botOwner_0.Steering.LookToPoint(_follower.LootingBrain.LootObjectPosition);
                // start looting the body
                if (_follower.LootingBrain.ActiveCorpse != null)
                    _follower.LootingBrain.StartLooting();
                // pick up the given item
                else
                {
                    PickUpItem().Forget();
                }

                bool_1 = true;
            }
        }

        private async UniTask PickUpItem()
        {
            await _follower.TransactionController.TryPickupItem(_follower.LootingBrain.ActiveItem.Item);
        }
    }
}
