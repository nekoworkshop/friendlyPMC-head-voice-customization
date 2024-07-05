using EFT;
using friendlyPMC.Modules;
using System.Collections.Generic;

namespace friendlyPMC.Components
{
    internal class FollowerEnemyChoose : GClass405
    {
        public FollowerEnemyChoose(BotOwner owner) : base(owner)
        {

        }

        public override EnemyInfo FindDangerEnemy()
        {
            List<EnemyInfo> list = new List<EnemyInfo>();

            foreach (EnemyInfo enemyInfo in botOwner_0.EnemiesController.EnemyInfos.Values)
            {
                if (!enemyInfo.Person.HealthController.IsAlive || !enemyInfo.HaveSeen || !enemyInfo.ShallKnowEnemy())
                {
                    continue;
                }

                if (enemyInfo.IsVisible || enemyInfo.HaveSeen) list.Add(enemyInfo);
            }

            if(BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.HaveBoss)
            {
                pitAIBossPlayer boss = (botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer);
                List<BotOwner> bossEnemies = boss.GetEnemies();
                foreach (var item in bossEnemies)
                {
                    if(!item.HealthController.IsAlive || item.BotState != EBotState.Active)
                    {
                        continue;
                    }
                    try
                    {
                        botOwner_0.EnemiesController.AddNew(boss.bossGroup, item, new BotSettingsClass(botOwner_0.AIData.Player, boss.bossGroup, EBotEnemyCause.checkAddTODO));

                        EnemyInfo info;
                        botOwner_0.EnemiesController.EnemyInfos.TryGetValue(item, out info);
                        if (info != null)
                        {
                            list.Add(info);
                        }
                    }
                    catch{ }
                }
            }

            return BetterEnemy(list, false);
        }
    }
}
