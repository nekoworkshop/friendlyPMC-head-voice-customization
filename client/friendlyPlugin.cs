using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using friendlyPMC.Patches;
using System.Reflection;
using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{
    [BepInPlugin("xyz.pit.companion", "[pit-friendlyPMC]", "1.0.0")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.4.0.0")]
    [BepInDependency("xyz.drakia.waypoints")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;
        private void Awake()
        {
            
            if(awaken) return;
            awaken = true;

            new Logger();

            new BossPlayers();
            new Receivers();
            new InteractableObjects();

            new PlayerPatch().Enable();

            new BotSpawnerAddPlayerPatch().Enable();

            new BotOwnerIsFolowerPatch().Enable();
            new BotOwnerDamagePatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();

            new BotReceiverInitPatch().Enable();
            new BotReceiverDisposePatch().Enable();
            new BotReceiverPhrasePatch().Enable();

            new QuickPanelPatch().Enable();

            new CreateNodePatch().Enable();


        }

    }
}
