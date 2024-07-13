using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerSniperSearch : GClass160
    {

        private CustomNavigationPoint Spot = null;

        private Vector3? spotPosition;

        private bool sprint = false;

        private float float_4 = 0f;
        private float float_5 = 0f;

        private NavMeshPath navMeshPath;
        public FollowerSniperSearch(BotOwner bot) : base(bot)
        {
            

            navMeshPath = new NavMeshPath();
        }

        public override void Update()
        {
            Components.Logger.LogInfo("sniperSearch: Do Sniper Search");
            
            botOwner_0.DoorOpener.Update();

            if (!botOwner_0.Memory.HaveEnemy) return;

            if(spotPosition.HasValue)
            {
                Components.Logger.LogInfo("sniperSearch: Have spot");

                if(botOwner_0.GoToSomePointData.IsCome())
                {
                    botOwner_0.Mover.Stop();
                    if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.Distance < 10f)
                    {
                        botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                    }

                    Spot = null;
                    spotPosition = null;

                    return;
                }

                
                Components.Logger.LogInfo("sniperSearch: go to point");
                if (float_4 < Time.time)
                {
                    float_4 = Time.time + 2f;
                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, spotPosition.Value, navMeshPath) > 20f;
                }
                botOwner_0.GoToSomePointData.UpdateToGo(sprint);

                return;
            }

            if (Spot == null && float_5 < Time.time)
            {

                Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

                Vector3 targetSpot = new Vector3(
                    Mathf.Floor(enemyPosition.x / 20f) * 20f,
                    Mathf.Floor(enemyPosition.y / 20f) * 20f,
                    Mathf.Floor(enemyPosition.z / 20f) * 20f
                );


                float_5 = Time.time + GClass760.Random(2f, 4f);

                Components.Logger.LogInfo("sniperSearch: look for a spot");
                // find a cover from where we can shoot the enemy
                Spot = Utils.Covers.GetClosestAttackCoverPoint(botOwner_0, targetSpot, 25f,100f,null,true);

                if(Spot != null)
                {
                    spotPosition = Spot.Position;
                }
                // else find a position from where we can see the enemy
                else
                {
                    ShootPointClass shootTarget = new ShootPointClass(targetSpot, 1f);

                    spotPosition = Utils.Covers.FindShootPosition(botOwner_0, shootTarget, 15f, 100f);
                }

                if(!spotPosition.HasValue)
                {
                    Components.Logger.LogInfo("sniperSearch: no sniping spot found");
                } else
                {
                    Components.Logger.LogInfo("sniperSearch: have spot");
                    botOwner_0.GoToSomePointData.SetPoint((Vector3)spotPosition);
                    bool sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, (Vector3)spotPosition) > 20f;
                    botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                }
            }

        }
    }
}
