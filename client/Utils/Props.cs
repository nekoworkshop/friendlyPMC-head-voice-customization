using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Utils
{
    internal class Props
    {
        private static float _def_coverSearchRadius = 80f;
        private static float _def_sprintDistance = 15f;
        private static float _def_regroupMinDistance = 7f;
        private static float _def_searchRadius = 50f;
        private static float _def_nearSearchRadius = 30f;

        private static float _def_bossInnerRadius = 30f;

        private static float _def_bossOuterRadius = 50f;

        private static float _def_bossMaxCoverDistance = 35f;
        private static float _def_bossMinCoverDistance = 10f;


        public static float coverSearchRadius;
        public static float sprintDistance;
        public static float regroupMinDistance;
        public static float searchRadius;
        public static float nearSearchRadius;

        public static float bossInnerRadius;

        public static float bossOuterRadius;

        public static float bossMaxCoverDistance;
        public static float bossMinCoverDistance;


        public static void Reset()
        {
            coverSearchRadius = _def_coverSearchRadius;
            sprintDistance = _def_sprintDistance;
            regroupMinDistance = _def_regroupMinDistance;
            searchRadius = _def_searchRadius;
            nearSearchRadius = _def_nearSearchRadius;
            bossInnerRadius = _def_bossInnerRadius;
            bossOuterRadius = _def_bossOuterRadius;
            bossMaxCoverDistance = _def_bossMaxCoverDistance;
            bossMinCoverDistance= _def_bossMinCoverDistance;
        }


        public static void FactoryMapSett()
        {
            regroupMinDistance = 7f;
            bossInnerRadius = 20f;
            bossOuterRadius = 40f;
            coverSearchRadius = 40f;
            searchRadius = 40f;

        }

    }
}
