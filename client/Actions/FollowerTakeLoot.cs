using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;

using UnityEngine;
using UnityEngine.AI;

using Cysharp.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;

namespace friendlyPMC.Actions
{
    internal class FollowerTakeLoot : BaseNodeClass
    {
        private BotFollowerPlayer _follower;

        private bool bool_0 = false;

        private bool bool_1 = false;    

        public FollowerTakeLoot(BotOwner bot) : base(bot)
        {
        }

        public void EnableTransactions()
        {
            _follower.LootingBrain.EnableTransactions();
            _follower.LootingBrain.UpdateGridStats();
        }

        public void DisableTransactions()
        {
            _follower.LootingBrain.DisableTransactions();
            _follower.LootingBrain.UpdateGridStats();
        }

        public override void Update()
        {
            if(_follower == null)
            {
                _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            }

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
                botOwner_0.SetPose(0f);

                // start looting the body
                if (_follower.LootingBrain.ActiveCorpse != null)
                {
                    botOwner_0.Steering.LookToPoint(_follower.LootingBrain.LootObjectPosition);
                    
                    var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(10.0), false);
                    Timer.OnTimer += () =>
                    {
                        try
                        {
                            DisableTransactions();
                            _follower.LootingBrain.StopAllCoroutines();
                            _follower.LootingBrain.ActiveItem = null;
                            _follower.LootingBrain.ActiveCorpse = null;
                            bool_0 = false;
                            bool_1 = false;
                        }
                        catch { }
                    };


                    EnableTransactions();
                    _follower.LootingBrain.StartCoroutine(_follower.LootingBrain.LootCorpse());
                }
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


            EnableTransactions();

            bool result = await _follower.TransactionController.TryPickupItem(_follower.LootingBrain.ActiveItem.Item);

            if(!result)
            {
                DisableTransactions();
                _follower.LootingBrain.StopAllCoroutines();
                _follower.LootingBrain.ActiveItem = null;
                _follower.LootingBrain.ActiveCorpse = null;
            }

            DisableTransactions();

            bool_0 = false;
            bool_1 = false;
        }
    }
}
