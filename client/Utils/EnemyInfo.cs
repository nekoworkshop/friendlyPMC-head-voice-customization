using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Utils
{
    internal class EnemyInfo
    {

        public enum EnemyDistance
        {
            VeryClose = 0,
            Close = 1,
            Mid = 2,
            Distant = 3,
            Far = 4
        }

        public enum ProxyDistance
        {
            VeryClose = 0,
            Close = 1,
            Mid = 2,
            Distant = 3,
            Far = 4
        }
        public static bool IsClose(BotOwner bot)
        {
            if (!bot.Memory.HaveEnemy) return false;

            return (Distance(bot) <= EnemyDistance.Close && bot.Memory.GoalEnemy.IsVisible) || Distance(bot) == EnemyDistance.VeryClose;
        }

        public static ProxyDistance DistanceProxy(BotOwner bot, Vector3 position)
        {
            Vector3 enemyPosition = bot.Memory.GoalEnemy.CurrPosition;

            float distance = Vector3.Distance(position, enemyPosition);

            if (distance < 12f) return ProxyDistance.VeryClose;

            if (distance < 30f)
            {
                return ProxyDistance.Close;
            }

            if (distance < 55f)
            {
                return ProxyDistance.Mid;
            }

            if (distance < 110f)
            {
                return ProxyDistance.Distant;
            }

            return ProxyDistance.Far;
        }
        public static EnemyDistance Distance(BotOwner bot)
        {
            if (!bot.Memory.HaveEnemy) return EnemyDistance.Far;
            
            Vector3 botPosition = bot.GetPlayer.Transform.position;
            Vector3 enemyPosition = bot.Memory.GoalEnemy.CurrPosition;

            float distance = Utils.GetNavDistance(botPosition, enemyPosition);

            if(distance < 12f) return EnemyDistance.VeryClose;

            if (distance < 30f)
            {
                return EnemyDistance.Close;
            }

            if (distance < 55f)
            {
                return EnemyDistance.Mid;
            }

            if (distance < 110f)
            {
                return EnemyDistance.Distant;
            }

            return EnemyDistance.Far;

        }

        public static float GetEnemiesAtLocation(BotOwner bot, Vector3 position, float radius = 25f)
        {
            float nr = 0;

            Collider[] hits = new Collider[100];

            int numHits = Physics.OverlapSphereNonAlloc(
                position,
                radius,
                hits,
                LayerMaskClass.PlayerMask
            );

            for (int i = 0; i < numHits; i++)
            {
                var enemy = bot.ShootData.method_4(hits[i]);

                if (enemy != null && enemy.IsAI && enemy.HealthController.IsAlive && (bot.EnemiesController.IsEnemy(enemy) || bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.Contains(enemy.GetPlayer.Profile.Info.Settings.Role)))
                {
                        nr++;
                }
            }

            return nr;
        }
    }
}
