using EFT;
using LootingBots.Patch.Util;
using RootMotion.FinalIK;
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
        public static bool CheckLook(Player from, Player to, BodyPartType botType = BodyPartType.head)
        {
            EnemyPart enemyPart = from.MainParts[botType];
            Vector3 direction = to.MainParts[BodyPartType.head].Position - enemyPart.Position;
            float magnitude = direction.magnitude;
            RaycastHit raycastHit;
            return Physics.Raycast(new Ray(enemyPart.Position, direction), out raycastHit, magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }
        public static void ScanDirection(BotOwner bot, IPlayer player, Player realPlayer)
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
                float edist = Vector3.Distance(playerPosition, item.Position);
                if (edist < dist && (CheckLook(item, realPlayer) || CheckLook(item, realPlayer, BodyPartType.body)))
                {
                    dist = edist;
                    closet = item;
                }
            }

            if ( closet != null )
            {
                if(bot.Memory.HaveEnemy)
                {
                    Components.Logger.LogInfo("Already engaged, add player's closest visible enemy to the group");
                    bot.BotsGroup.AddEnemy(closet.AIData.Player, EBotEnemyCause.addBotAtGroup);
                    return;
                }

                Components.Logger.LogInfo("Try make player's closest visible enemy the priority");

                bot.BotsGroup.AddEnemy(closet.AIData.Player, EBotEnemyCause.addBotAtGroup);

                EnemyInfo info;
                bot.EnemiesController.EnemyInfos.TryGetValue(closet, out info);
                if (info != null)
                {
                    Components.Logger.LogInfo("Made closest enemy a priority");
                    bot.Memory.GoalEnemy = info;
                }
            }
        }
    }
}
