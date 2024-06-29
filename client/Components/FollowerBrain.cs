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
        public FollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner)
        {
            AddLayers();

            _boss = boss;

            owner.GetPlayer.HealthController.DiedEvent += OnDead;
            owner.LeaveData.OnLeave += OnLeave;
            owner.Memory.OnAddEnemy += OnAddEnemy;

        }
        /** Exposed method for adding brain layers so it can be patched by addons **/
        public virtual void AddLayers()
        {
            // order matters for which layer get the initial priority
            // - follow
            FollowerLayer followLayer = new FollowerLayer(_owner, 51);
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
            FollowerLootLayer layer9 = new FollowerLootLayer(_owner, 50);
            method_0(8, layer9, true);
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
            BossPlayers.Instance.RemoveFollower(_owner, _boss);
            ClearFollowerPatrol();
            InteractableObjects.ClearStoredItems(_owner.ProfileId);

        }

        public virtual void OnLeave(BotOwner _bot)
        {
            BossPlayers.Instance.RemoveFollower(_owner, _boss);
            ClearFollowerPatrol();
            InteractableObjects.ClearStoredItems(_owner.ProfileId);
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
                fightLayer.SetBossFightTactic(tactic);
                BossOrdersChanged();
            }
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
