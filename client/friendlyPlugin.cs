using BepInEx;
using friendlyPMC.Components;
using friendlyPMC.Patches;
using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{
    [BepInPlugin("xyz.pit.companion", "[pit-friendlyPMC]", "0.1")]
    [BepInDependency("xyz.drakia.bigbrain", "0.4.0.0")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    public class friendlyPMC : BaseUnityPlugin
    {
        private void Awake()
        {

            new Logger();

            new BossPlayer();

            new PlayerPatch().Enable();

            new BotSpawnerAddPlayerPatch().Enable();

            new BotOwnerIsFolowerPatch().Enable();
            new BotOwnerDamagePatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();
            new ActivateGoToCheckRequestPatch().Enable();
            new ActivateGoToPointRequestPatch().Enable();

            new CreateNodePatch().Enable();
        }

    }
}
