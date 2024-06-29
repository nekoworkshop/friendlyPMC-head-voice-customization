using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Components.BossFollower;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeFollowLayer: BossFollowLayer
    {
        public BirdEyeFollowLayer(BotOwner bot, int priority) : base(bot, priority)
        {
        }

        public override string Name()
        {
            return "BirdEyeFLP";
        }

        public override bool ShallUseNow()
        {

            bool usage = base.ShallUseNow();

            if(usage && botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon)
            {
                botOwner_0.WeaponManager.Selector.TryChangeToMain();
            }

            return usage;
        }
    }
}
