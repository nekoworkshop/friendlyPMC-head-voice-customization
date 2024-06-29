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
            Vector3 direction = to.PlayerBones.Head.position - enemyPart.Position;
            float magnitude = direction.magnitude;
            RaycastHit raycastHit;
            return Physics.Raycast(new Ray(enemyPart.Position, direction), out raycastHit, magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }
        public static void ScanDirection(BotOwner bot, IPlayer player, Player realPlayer)
        {

            List<Player> enemies = new List<Player>();

            float scanDistance = friendlyPMC.scanDistance.Value;

            if (bot.IsRole(WildSpawnType.followerBirdEye)) scanDistance = 300f;

            Vector3 playerPosition = player.Transform.position;
            Vector3 playerLookDirection = player.LookDirection;
            float sphereRadius = scanDistance / 2;
            float sphereDistance = scanDistance / 2;

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

            if ( closet != null)
            {

                if(bot.Memory.HaveEnemy)
                {
                    bot.BotsGroup.AddEnemy(closet, EBotEnemyCause.checkAddTODO);
                    return;
                }

                bot.BotsGroup.AddEnemy(closet.AIData.Player, EBotEnemyCause.checkAddTODO);
                bot.Memory.AddEnemy(closet, new BotSettingsClass(closet, bot.BotsGroup, EBotEnemyCause.checkAddTODO), false);
                EnemyInfo info;
                bot.EnemiesController.EnemyInfos.TryGetValue(closet, out info);
                if (info != null)
                {
                    info.PriorityIndex = 0;
                    bot.Memory.GoalEnemy = info;
                    info.SetVisible(true);
                }
            }
        }
    }
}
