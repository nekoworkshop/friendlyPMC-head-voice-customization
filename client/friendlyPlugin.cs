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
        private void Awake()
        {
            new Logger();
            
            if(BossPlayers.Instance == null) { 
                new BossPlayers();
                new Receivers();
            }

            new PlayerPatch().Enable();

            new BotSpawnerAddPlayerPatch().Enable();

            new BotOwnerIsFolowerPatch().Enable();
            new BotOwnerDamagePatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();

            new BotRecieverInitPatch().Enable();
            new BotRecieverDisposePatch().Enable();
            new BotReceiverPhrasePatch().Enable();

            new QuickPanelPatch().Enable();


        }

    }
}
