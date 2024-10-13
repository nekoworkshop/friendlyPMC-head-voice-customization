using EFT;
using friendlyPMC.Components.Tactics;
using friendlyPMC.Utils;
using System.Collections.Generic;
using static Koenigz.PerfectCulling.EFT.PerfectCullingTreePreProcess;

namespace friendlyPMC.Components
{
    internal class FollowerAvoidDanger : GClass36
    {

        private FollowerCommonLayer commonLayer;

        bool btrRegroup = false;
        
        List<string> regroupDecisions = new List<string>
        {
            "moveCloserToBoss",
            "moveCloserToBossFast",
            "regroupToBossFast",
            "regroupToBoss"
        };

        public FollowerAvoidDanger(BotOwner bot, int priority) : base(bot, priority)
        {
            commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override bool ShallUseNow()
        {
            bool use = base.ShallUseNow();
            if (btrRegroup)
            {
                use = true;
            }


            if (use == false) btrRegroup = false;

            return use;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if (request != null && request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
            {
                btrRegroup = true;
                Utils.Utils.SetTimeout(() =>
                {
                    BotRequest req = botOwner_0.BotRequestController.CurRequest;

                    if (botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && req != null && req.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
                    {
                        btrRegroup = false;
                        req.Complete();
                    }

                }, 2500);

                return BotLogicDecisions.RegroupToBoss(botOwner_0);
            }

            return base.GetDecision();
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            
            AICoreActionEndStruct result = base.ShallEndCurrentDecision(curDecision);
            if(result.Value && !regroupDecisions.Contains(curDecision.Reason))
            {
                btrRegroup = false;
            }
            return result;
        }
    }
}
