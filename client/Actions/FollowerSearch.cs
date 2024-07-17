using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace friendlyPMC.Actions
{
    internal class FollowerSearch : FollowerSniperSearch
    {
        public FollowerSearch([NotNull] BotOwner owner)
        : base(owner)
        {
            _minDist = 5f;
            _maxDist = 60f;
        }
    }
}
