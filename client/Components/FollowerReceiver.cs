using EFT;

namespace friendlyPMC.Components
{
    internal class FollowerReceiver : BotReceiver
    {
        public FollowerReceiver(BotOwner owner) : base(owner)
        {

        }

        public new void method_0(BotEventHandler.GClass599 info)
        {
            Logger.LogInfo("phase was " + info.phrase.ToString());
            base.method_0(info);
        }
    }
}
