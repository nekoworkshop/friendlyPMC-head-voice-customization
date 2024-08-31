using EFT;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerEnemyScan
    {
        public static bool CheckLook(Player from, Player to, LayerMask mask, BodyPartType botType = BodyPartType.head)
        {
            if (Vector3.Dot((to.Transform.position - from.Transform.position), to.LookDirection) > 0f) return false;

            EnemyPart enemyPart = from.MainParts[botType];
            Vector3 direction = to.PlayerBones.Head.position - enemyPart.Position;
            float magnitude = direction.magnitude;
            RaycastHit raycastHit;
            return Physics.Raycast(new Ray(enemyPart.Position, direction), out raycastHit, magnitude, mask);
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

            RaycastHit[] hits = new RaycastHit[100];

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
                if (edist < dist && (CheckLook(item, realPlayer,bot.LookSensor.Mask) || CheckLook(item, realPlayer,bot.LookSensor.Mask, BodyPartType.body)))
                {
                    dist = edist;
                    closet = item;
                }
            }

            if (closet != null)
            {
                Components.Logger.LogInfo("Player has seen " + closet.Profile.Nickname);

                if (bot.Memory.HaveEnemy && bot.Memory.GoalEnemy.ProfileId == closet.Profile.ProfileId) return;

                EnemyInfo info = Utils.Enemy.MakeEnemy(bot, closet);

                if (info != null && !bot.Memory.HaveEnemy)
                {
                    info.PriorityIndex = 0;
                    info.SetVisible(true);
                    bot.Memory.GoalEnemy = info;

                    Components.Logger.LogInfo("Made " + closet.Profile.Nickname + " an active enemy to " + bot.Profile.Nickname);
                }
                else if (info == null)
                {
                    Components.Logger.LogInfo("Cannot make " + bot.Profile.Nickname + " an active enemy");
                }
            }
        }
    }
}
