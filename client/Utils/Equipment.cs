using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Utils
{
    internal class Equipment
    {
        private static List<GClass3582> _EquipmentPresets = new List<GClass3582>();

        public static List<GClass3582> CustomPresets
        {
            get { return _EquipmentPresets; }
        }
    }
}
