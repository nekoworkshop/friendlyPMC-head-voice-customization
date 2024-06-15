using EFT;
using LootingBots.Patch.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerEnemyScan
    {
        public static void ScanDirection(BotOwner bot, IPlayer player)
        {
            Vector3 direction = player.LookDirection;
            
            Components.Logger.LogInfo("Scan for Enemy");

            RaycastHit[] hits = Physics.RaycastAll(player.Transform.position, direction, 110f, LayerMaskClass.PlayerMask);

            foreach (RaycastHit hit in hits)
            {
                // Check if the hit object is an enemy
                if (hit.collider != null)
                {
                    var enemy = bot.ShootData.method_4(hit.collider);
                    if (enemy != null && enemy.HealthController.IsAlive && enemy.Side != player.Side)
                    {
                        Components.Logger.LogInfo("Enemy Found");
                        bot.BotsGroup.AddEnemy(enemy, EBotEnemyCause.initCauseEnemy);
                        break;
                    }
                }
            }
        }
    }
}
