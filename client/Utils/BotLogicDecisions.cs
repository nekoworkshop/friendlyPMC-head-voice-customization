using EFT;
using UnityEngine;

namespace friendlyPMC.Utils
{
    internal class BotLogicDecisions
    {
        public static readonly float sprintDistance = 15f;
        public static AICoreActionResultStruct<BotLogicDecision> RegroupToBoss(BotOwner bot)
        {

            BotRequest request = bot.BotRequestController.CurRequest;

            IPlayer requester = request != null ? bot.BotRequestController.CurRequest.Requester : null;

            if(requester == null) {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "backToBoss");
            }

            Vector3 requestPos = requester.Transform.position;

            float randomX = GClass760.Random(-5f, 5f);
            float randomZ = GClass760.Random(-5f, 5f);
            Vector3 offset = new Vector3(randomX, 0f, randomZ);

            Vector3 finPos = requestPos + offset;
            finPos.y = requester.PlayerBody.PlayerBones.Head.position.y;

            Vector3 point = new Vector3(finPos.x, requestPos.y, finPos.z);

            bot.GoToSomePointData.SetPoint(point);

            bool shouldSprint01 = Vector3.Distance(point, bot.GetPlayer.Transform.position) >= sprintDistance;
            bot.GoToSomePointData.UpdateToGo(shouldSprint01);
            if (!shouldSprint01) bot.Sprint(false);

            request.Complete();

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, shouldSprint01 ? "regroupToBossFast" : "regroupToBoss");
        }
    }
}
