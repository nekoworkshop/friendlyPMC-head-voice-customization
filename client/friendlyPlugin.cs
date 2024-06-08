using BepInEx;

using friendlyPMC.Patches;
using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{
    [BepInPlugin("xyz.pit.companion", "[pit-friendlyPMC]", "1.0.0")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.4.0.0")]
    [BepInDependency("xyz.drakia.waypoints")]
    [BepInDependency("com.Arys.UnityToolkit")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;
        private void Awake()
        {

            if (!awaken)
            {
                awaken = true;
                new Logger();
            }

            new AIBossPlayerDisposePatch().Enable();


            new BotSpawnerAddPlayerPatch().Enable();

            new BotGroupIsEnemyPatch().Enable();
            new BotEnemiesControllerPatch().Enable();
            new BotOwnerDamagePatch().Enable();

            
            new BotOwnerIsFolowerPatch().Enable();
            new PatrolDataFollowerPatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();


            
            new BotReceiverInitPatch().Enable();
            new BotReceiverDisposePatch().Enable();
            new BotReceiverPhrasePatch().Enable();

            new QuickPanelPatch().Enable();

            new CreateNodePatch().Enable();
            //new GetClosePointsPatch().Enable();

            new BotsControllerPatch().Enable();
            new LocalGamePatch().Enable();
        }

    }
}
