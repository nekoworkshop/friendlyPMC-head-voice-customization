using EFT;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Utils
{
    internal class BotLogicDecisions
    {
        public static readonly float sprintDistance = 15f;
        public static AICoreActionResultStruct<BotLogicDecision> RegroupToBoss(BotOwner bot)
        {

            BotRequest request = bot.BotRequestController.CurRequest;

            IPlayer requester = request != null ? bot.BotRequestController.CurRequest.Requester : null;

            if(requester == null && bot.BotFollower.HaveBoss)
            {
                requester = bot.BotFollower.BossToFollow.Player();
            }

            if(requester == null) {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "requester.None");
            }

            Vector3 requestPos = requester.Transform.position;

            NavMeshPath mesh = new NavMeshPath();

            // try to find a valid position within the sphere
            Vector3? finPos = null;
            for (int i = 0; i < 11; i++) // Adjust the number of attempts as needed
            {
                Vector3 randomPosition = requestPos + UnityEngine.Random.insideUnitSphere * Props.bossInnerRadius;

                NavMeshHit navMeshHit;

                if (!NavMesh.SamplePosition(randomPosition, out navMeshHit, 10f, -1)) continue;

                //if (!Covers.IsNavigablePoint(bot.GetPlayer.Transform.position, navMeshHit.position, 150f, mesh)) continue;
                if (!finPos.HasValue)
                {
                    finPos = navMeshHit.position;
                } else if((requestPos - navMeshHit.position).sqrMagnitude < (requestPos - finPos.Value).sqrMagnitude)
                {
                    finPos = navMeshHit.position;
                }
            }
            // no valid point found
            if(!finPos.HasValue)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "requester.badPosition");
            }

            bot.GoToSomePointData.SetPoint(finPos.Value);

            bool shouldSprint01 = Vector3.Distance(finPos.Value, bot.GetPlayer.Transform.position) >= sprintDistance;
            bot.GoToSomePointData.UpdateToGo(shouldSprint01);
            if (!shouldSprint01) bot.Sprint(false);

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, shouldSprint01 ? "regroupToBossFast" : "regroupToBoss");
        }
    }
}
