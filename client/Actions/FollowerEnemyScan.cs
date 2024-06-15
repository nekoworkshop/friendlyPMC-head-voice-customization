using EFT;
using LootingBots.Patch.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace friendlyPMC.Actions
{
    internal class FollowerEnemyScan
    {
        public static void ScanDirection(BotOwner bot, IPlayer player)
        {
            Components.Logger.LogInfo("Scan for Enemies");

            List<Player> enemies = new List<Player>();

            Vector3 playerPosition = player.Transform.position;
            Vector3 playerLookDirection = player.LookDirection;
            float sphereRadius = friendlyPMC.scanDistance.Value / 2;
            float sphereDistance = friendlyPMC.scanDistance.Value / 2;

            RaycastHit[] hits = new RaycastHit[10];

            int numHits = Physics.SphereCastNonAlloc(
                    new Ray(playerPosition, playerLookDirection),
                    sphereRadius,
                    hits,
                    sphereDistance,
                     LayerMaskClass.PlayerMask
                );

            // get all enemies the boss might have seen
            for (int i = 0; i < numHits; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider != null)
                {
                    var enemy = bot.ShootData.method_4(hit.collider);
                    
                    if (enemy != null && enemy.IsAI && enemy.HealthController.IsAlive && enemy.Side != player.Side)
                    {
                        Components.Logger.LogInfo("Add enemy to the list");
                        enemies.Add(enemy);
                    }
                }
            }
            // make the close one the priority
            float dist = Mathf.Infinity;
            Player closet = null;
            foreach (var item in enemies)
            {
                float edist = Vector3.Distance(player.Transform.position, item.Position);
                if (edist < dist)
                {
                    dist = edist;
                    closet = item;
                }
            }

            if ( closet != null )
            {
                bot.BotsGroup.AddEnemy(closet.AIData.Player, EBotEnemyCause.addBotAtGroup);
                
                if (bot.Memory.HaveEnemy) return;

                Components.Logger.LogInfo("Add closest enemy to the group");
                Vector3 direction = closet.Position - playerPosition;
                float distance = direction.magnitude;
                RaycastHit hit;

                if(Physics.Raycast(player.PlayerBones.Head.position, direction,out hit, distance, LayerMaskClass.HighPolyWithTerrainMask)) {
                    if(hit.collider != null)
                    {
                        var aiHit = bot.ShootData.method_4(hit.collider);
                        if (aiHit != null && aiHit.ProfileId == closet.ProfileId)
                        {
                            EnemyInfo info;
                            bot.EnemiesController.EnemyInfos.TryGetValue(aiHit.AIData.Player, out info);
                            if(info != null)
                            {
                                Components.Logger.LogInfo("Make closest enemy a priority");
                                //info.SetVisible(true);
                                bot.Memory.GoalEnemy = info;
                            }
                        }
                    }
                }
                
            }
        }
    }
}
