using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using System;
using UnityEngine;

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

        public bool needsProtection
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

        public FollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner)
        {
            AddLayers();

            _boss = boss;

            owner.GetPlayer.HealthController.DiedEvent += OnDead;
            owner.LeaveData.OnLeave += OnLeave;
            owner.Memory.OnAddEnemy += OnAddEnemy;


            _currentTactic = "Default";

        }
        /** Exposed method for adding brain layers so it can be patched by addons **/
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

            // - grenade
            GClass36 layer = new GClass36(_owner, 130);
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

        public virtual void OnDead(EDamageType damageType)
        {
            OnKilled();

        }

        public virtual void OnLeave(BotOwner _bot)
        {
            OnKilled();
        }

        protected void OnKilled()
        {


            // clear info about this bot
            NpcMessage.RemoveNpc(_owner.ProfileId);
            InteractableObjects.ClearStoredItems(_owner.ProfileId);
            InteractableObjects.RemoveTaker(_owner);

            BossPlayers.RemoveFollower(_owner, _boss);
            ClearFollowerPatrol();
        }


        public virtual void OnAddEnemy(IPlayer player)
        {
            // how does the boss get added as Enemy?? - fix it
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
            Dismissed();

            // clear info about this bot
            NpcMessage.RemoveNpc(_owner.ProfileId);
            InteractableObjects.ClearStoredItems(_owner.ProfileId);
            InteractableObjects.RemoveTaker(_owner);

            base.Dispose();
        }

        public virtual void Dismissed()
        {
            ClearFollowerPatrol();

            _owner.GetPlayer.HealthController.DiedEvent -= OnDead;
            _owner.LeaveData.OnLeave -= OnLeave;
            _owner.Memory.OnAddEnemy -= OnAddEnemy;
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
