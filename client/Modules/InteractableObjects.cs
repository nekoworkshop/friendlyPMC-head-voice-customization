using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Modules
{
    internal class InteractableObjects
    {
        public static InteractableObjects Instance;

        private Corpse _currCorpse;

        private Door _currDoor;


        public InteractableObjects() { 
            if(Instance == null)
            {
                Instance = this;
            }
        }


        public static void SetCurCorpse(Corpse corpse)
        {
            Instance._currCorpse = corpse;
        }

        public static Corpse GetCurCorpse()
        {
            return Instance._currCorpse;
        }

        public static void SetCurDoor(Door door) {
        
            Instance._currDoor = door;
        }
        public static Door GetCurDoor()
        {
            return Instance._currDoor;
        }
    }
}
