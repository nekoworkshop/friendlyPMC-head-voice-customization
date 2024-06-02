using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Components
{
    internal class FollowerMeds : GClass413
    {
        public FollowerMeds(BotOwner owner, Action<bool> callback)
        : base(owner, callback)
        {
        }
        private void GiveGrizzlyORIFK()
        {

        }
        public override void Activate()
        {
            
            base.Activate();
        }

        public override void RefreshMeds()
        {
            Player getPlayer = this.botOwner_0.GetPlayer;
            EquipmentSlot[] array = BotMedecine.anySlots;
            this.list_0.Clear();
            getPlayer.GClass2761_0.GetAcceptableItemsNonAlloc<MedsClass>(array, this.list_0, null);
            List<GClass2726> list = this.list_0.OfType<GClass2726>().ToList<GClass2726>();
            GClass2726 gclass = list.FirstOrDefault(new Func<GClass2726, bool>(GClass412.Class212.class212_0.method_0));
            if (gclass != null)
            {
                base.CurUsingMeds = gclass;
                return;
            }
            base.CurUsingMeds = list.FirstOrDefault<GClass2726>();
        }
    }
}
