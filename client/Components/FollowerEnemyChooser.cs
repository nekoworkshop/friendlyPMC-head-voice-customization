using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class FollowerEnemyChooser : BotEnemyChooser
    {
        public FollowerEnemyChooser(BotOwner owner) : base(owner) 
        { 
        }

        public override EnemyInfo FindDangerEnemy()
        {

            List<EnemyInfo> list = new List<EnemyInfo>();
            foreach (EnemyInfo enemyInfo in this.botOwner_0.EnemiesController.EnemyInfos.Values)
            {
                
                if (!enemyInfo.Person.HealthController.IsAlive)
                {
                    continue;
                }
                else if (!enemyInfo.HaveSeen)
                {
                    continue;
                }
                else if (!enemyInfo.ShallKnowEnemy())
                {
                    continue;
                }
                else
                {
                    list.Add(enemyInfo);
                }
            }
 
            if (list.Count == 0)
            {
                return null;
            }
            return BetterEnemy(list);
        }
        public EnemyInfo BetterEnemy(List<EnemyInfo> enemiesInfos)
        {
            if (enemiesInfos.Count == 0)
            {
                return null;
            }

            BotEnemiesController enemiesController = this.botOwner_0.EnemiesController;
            float num = float.MaxValue;
            IPlayer player = null;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;

            foreach (EnemyInfo enemyInfo in enemiesInfos)
            {
                float distance = enemyInfo.Distance;
                float elevationDifference = Mathf.Abs(botPosition.y - enemyInfo.CurrPosition.y);
                float navDistance = Utils.Utils.GetNavDistance(botPosition, enemyInfo.CurrPosition);
                /*if (
                    !enemyInfo.IsVisible && 
                    (distance < 30f || (elevationDifference >= 12f && elevationDifference < 25f && distance < 51f)) && 
                    navDistance > 49f
                )
                {
                    continue;
                }*/
                // ignore enemies that the bot cannot see, they can't see him, and are at a relative distance
                if (
                    enemyInfo.Owner && 
                    !enemyInfo.IsVisible && !botOwner_0.LookSensor.CheckLookSimple(enemyInfo.Owner.GetPlayer,botOwner_0.GetPlayer) &&
                    navDistance > 30f
                ) continue;


                if (enemyInfo.IgnoreUntilAggression)
                {
                    continue;
                }
                else
                {
                    
                    if (distance >= this.botOwner_0.Settings.FileSettings.Mind.MAX_AGGRO_BOT_DIST && enemyInfo.IsVisible && distance > this.botOwner_0.Settings.FileSettings.Mind.MAX_AGGRO_BOT_DIST_UPPER_LIMIT)
                    {
                        continue;
                    }
                    else
                    {
                        float num2 = this.CalcWeight(distance, enemyInfo);
                        if (num2 < num)
                        {
                            num = num2;
                            player = enemyInfo.Person;
                        }
                    }
                }
            }
            if (player == null)
            {
                return null;
            }
            
            return enemiesController.EnemyInfos[player];
        }
    }
}
