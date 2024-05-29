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


        public InteractableObjects() { 
            if(Instance == null)
            {
                Instance = new InteractableObjects();
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

    }
}
