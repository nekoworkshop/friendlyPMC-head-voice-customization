using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Utils
{
    internal class Equipment
    {
        private static List<GClass3205> _EquipmentPresets = new List<GClass3205>();

        public static List<GClass3205> CustomPresets
        {
            get { return _EquipmentPresets; }
        }
    }
}
