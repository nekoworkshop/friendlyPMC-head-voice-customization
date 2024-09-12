using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace friendlyPMC.Components
{
    internal class FollowerBrain : BaseBrain
    {
        FollowerFightLayer fightLayer;

        protected pitAIBossPlayer _boss;

        protected string _currentTactic = null;
        protected string _defaultTactic = null;

        public string currentTactic
        {
            get
            {
                return _currentTactic;
            }
        }

        protected bool _needsProtection = true;

        public bool bossNeedsProtection
        {
            get
            {
                return _needsProtection;
            }

            set
            {
                _needsProtection = value;
                if (fightLayer != null)
                {
                    fightLayer.CoverType(value ? "close" : "far");
                }
            }
        }

        private float _gotShot = 0f;

        private float _underFire = 0f;
        
        public bool WasHit
        {
            get { return _gotShot > Time.time; }
        }

        public bool UnderFire
        {
            get { return _underFire > Time.time; }
        }

        private List<Vector3> processedSoundPositions = new List<Vector3>();

        private float _lastSoundTime = 0f;

        private float _lastGunshotTime = 0f;

        public FollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner)
        {
            AddLayers();

            _boss = boss;

            owner.GetPlayer.HealthController.DiedEvent += OnDead;
            owner.LeaveData.OnLeave += OnLeave;
            owner.Memory.OnAddEnemy += OnAddEnemy;
            owner.GetPlayer.BeingHitAction += BeingHitAction;

            _currentTactic = "Default";

        }
        
        public virtual void AddLayers()
        {
            // order matters for which layer get the initial priority
            // - follow
            FollowerLayer followLayer = new FollowerLayer(_owner, 50);
            method_0(1, followLayer, true);
            // - requests
            FollowerRequestLayer layer4 = new FollowerRequestLayer(_owner, 55);
            method_0(2, layer4, true);

            // - fight
            FollowerFightLayer layer6 = new FollowerFightLayer(_owner, 60);
            fightLayer = layer6;
            method_0(3, layer6, true);

            // - grenade and BTR
            GClass36 layer = new FollowerAvoidDanger(_owner, 130);
            method_0(4, layer, true);
            // - weapon malfunction
            GClass98 layer3 = new GClass98(_owner, 88);
            method_0(5, layer3, true);
            // - stay at position in prone mode
            GClass104 layer8 = new GClass104(_owner, 10, false, CoverLevel.Lay);
            method_0(7, layer8, true);
            // - item taker
            FollowerLootLayer layer9 = new FollowerLootLayer(_owner, 51);
            method_0(8, layer9, true);
            // - door opener 
            FollowerDoorLayer layer10 = new FollowerDoorLayer(_owner, 52);
            method_0(9, layer10, true);
        }

        public override string ShortName()
        {
            return "FLBPlayer";
        }

        public override GClass578 EventsPriority()
        {
            return new GClass578(1, 75, 45, 76);
        }

        protected virtual void OnDead(EDamageType damageType)
        {
            OnKilled();

        }
        // taken from SAIN
        protected float CalcTurnSpeed(Vector3 currLookDirection, Vector3 targetDirection)
        {
            float min = 125f;
            float max = 360f;
            float maxAngle = 150f;
            float minAngle = 5f;

            float angle = Vector3.Angle(currLookDirection, targetDirection.normalized);

            if (angle >= maxAngle)
            {
                return max;
            }
           
            if (angle <= minAngle)
            {
                return min;
            }

            float angleDiff = maxAngle - minAngle;
            float targetDiff = angle - minAngle;
            float ratio = targetDiff / angleDiff;
            float result = Mathf.Lerp(min, max, ratio);
            return result;
        }

        /** Bot should turn to the direction from where he got shot, if he is nor already in combat */
        protected void BeingHitAction(DamageInfo damageInfo, EBodyPart bodyType, float damageReducedByArmor)
        {
            if (!_owner.Memory.HaveEnemy && damageInfo.Player != null)
            {
                if(_owner.BotFollower.HaveBoss)
                {
                    if(_owner.BotFollower.BossToFollow.Player().ProfileId == damageInfo.Player.iPlayer.ProfileId) return;
                    if (_owner.BotFollower.BossToFollow.Followers.Find(bt => bt.ProfileId == damageInfo.Player.iPlayer.ProfileId)) return;
                }

                Vector3? pos = damageInfo.Player.iPlayer?.Position;
                
                if (pos.HasValue)
                {
                    try
                    {
                        Vector3 direction = pos.Value - _owner.GetPlayer.Transform.position;
                        
                        if (direction.sqrMagnitude < 1f)
                        {
                            direction = direction.normalized;
                        }

                        direction = direction * 20f; // ensure the bot will not look down at the ground

                        _owner.Steering.LookToPoint(direction, CalcTurnSpeed(_owner.LookDirection, direction));
                        
                        if(_gotShot > Time.time && _underFire < Time.time)
                        {
                            _underFire = Time.time + 5f;
                            _owner.Memory.SetUnderFire(damageInfo.Player.iPlayer);
                            _owner.CalcGoal();
                        }

                        _gotShot = Time.time + 3f;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public virtual void FakeShot(Vector3 direction)
        {
            _gotShot = Time.time + 3f;
            _owner.Steering.LookToPoint(direction, CalcTurnSpeed(_owner.LookDirection, direction));
        }

        /** On enemy sound heard make the bot either look towards the direction of the enemy or automatically make the enemy a target **/
        public virtual void SoundHeard(Player enemy,Vector3 position, float distance, AISoundType type)
        {
           
            if((type == AISoundType.silencedGun || type == AISoundType.gun) && Time.time < _lastGunshotTime + 3f) return;

            // on gun shot if there is a line of sight, turn immmediately
            if((type == AISoundType.silencedGun || type == AISoundType.gun) && !WasHit) 
            {
                if(distance <= 20f) Utils.Enemy.MakeEnemy(_owner, enemy);
                else if(
                    GClass301.CanShootToTarget(new ShootPointClass(_owner.GetPlayer.MainParts[BodyPartType.head].Position,1f),enemy.PlayerBones.WeaponRoot.position,_owner.LookSensor.Mask) ||
                    GClass301.CanShootToTarget(new ShootPointClass(_owner.GetPlayer.MainParts[BodyPartType.head].Position,1f), enemy.PlayerBones.WeaponRoot.position, _owner.LookSensor.Mask)
                ) {
                    Vector3 shootdir = position - _owner.GetPlayer.Transform.position;

                    if (shootdir.sqrMagnitude < 1f)
                    {
                        shootdir = shootdir.normalized;
                    }
                    
                    shootdir *= 20f; // ensure the bot will not look down at the ground

                    FakeShot(shootdir);
                    _lastGunshotTime = Time.time;
                    return;
                }
            }
            // turn and face the step sound 
            else if(type == AISoundType.step) 
            {
                 Vector3 positionZone = new Vector3(
                    Mathf.Floor(position.x / 18f) * 18f,
                    Mathf.Floor(position.y / 18f) * 18f,
                    Mathf.Floor(position.z / 18f) * 18f
                );

                bool wasProcessed = processedSoundPositions.Contains(positionZone);

                if(wasProcessed && Time.time - _lastSoundTime > 5f ) return;

                if(distance <= 10f) Utils.Enemy.MakeEnemy(_owner, enemy);
                else {
                    _lastSoundTime = Time.time;

                    Vector3 dir = position - _owner.GetPlayer.Transform.position;

                    if (dir.sqrMagnitude < 1f)
                    {
                        dir = dir.normalized;
                    }
                    
                    dir *= 20f; // ensure the bot will not look down at the ground

                    if(!wasProcessed) {
                        processedSoundPositions.Add(positionZone);
                        if(processedSoundPositions.Count > 20) processedSoundPositions.RemoveAt(0);
                    }
                    
                    FakeShot(dir);
                }
            }
        }

        /** On Leave info about this bot should be cleared */
        public virtual void OnLeave(BotOwner _bot)
        {
            OnKilled();
        }
        /** On Death info about this bot should be cleared */
        protected void OnKilled()
        {
            // remove this bot from being a follower
            Dismissed();
            BossPlayers.RemoveFollower(_owner, _boss);
        }


        protected virtual void OnAddEnemy(IPlayer player)
        {
            // how does the boss get added as Enemy here ?? - fix it
            if (player != null && player.ProfileId == _boss.Player().ProfileId)
            {
                _owner.Memory.DeleteInfoAboutEnemy(player);
                _owner.BotsGroup.RemoveEnemy(player);
                _owner.BotsGroup.AddAlly((Player)_boss.Player());
            }
        }

        public void ClearFollowerPatrol()
        {
            var patrols = FollowerPatrolInstances.GetPatrols();
            foreach (var item in patrols)
            {
                if (item.botOwner.ProfileId == _owner.ProfileId)
                {
                    patrols.Remove(item);
                    break;
                }
            }
        }
       
        public override void Dispose()
        {
            // remove this bot from being a follower
            BossPlayers.RemoveFollower(_owner, _boss);

            base.Dispose();
        }

        public virtual void Dismissed()
        {

            // delete his patrol data
            ClearFollowerPatrol();

            _owner.GetPlayer.HealthController.DiedEvent -= OnDead;
            _owner.LeaveData.OnLeave -= OnLeave;
            _owner.Memory.OnAddEnemy -= OnAddEnemy;
            _owner.GetPlayer.BeingHitAction -= BeingHitAction;

            // clear info about this bot
            InteractableObjects.ClearStoredItems(_owner.ProfileId);
            InteractableObjects.RemoveTaker(_owner);
            NpcMessage.RemoveNpc(_owner.ProfileId);
        }

        public virtual void SetBossTactic(string tactic)
        {
            if (fightLayer != null)
            {
                // whatever tactic we initially set when calling AddBotFollower, that becomes the default one
                if (_defaultTactic == null && tactic != null) _defaultTactic = tactic;
                else if(tactic == null && _defaultTactic != null) tactic = _defaultTactic;

                fightLayer.SetBossFightTactic(tactic);
                BossOrdersChanged();
            }
        }

        public virtual void SetTactic(string tactic)
        {
            _currentTactic = tactic;
        }

        public virtual void BossOrdersChanged()
        {
            if (fightLayer != null)
            {
                fightLayer.OrdersChanged();
            }
        }
    }
}
