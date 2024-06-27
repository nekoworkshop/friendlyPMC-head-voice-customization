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

            if (distance < 65f)
            {
                return ProxyDistance.Mid;
            }

            if (distance < 120f)
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

            if (distance < 56f)
            {
                return EnemyDistance.Mid;
            }

            if (distance < 120f)
            {
                return EnemyDistance.Distant;
            }

            return EnemyDistance.Far;

        }
    }
}
