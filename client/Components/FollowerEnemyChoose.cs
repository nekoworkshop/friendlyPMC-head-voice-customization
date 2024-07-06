using Comfort.Common;
using EFT;
using friendlyPMC.Modules;
using Sirenix.Serialization.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class FollowerEnemyChoose : GClass405
    {
        public FollowerEnemyChoose(BotOwner owner) : base(owner)
        {

        }

        public override EnemyInfo FindDangerEnemy()
        {
            EnemyInfo enemy = base.FindDangerEnemy();
            
            /*if(enemy != null && ((enemy.Owner != null && enemy.Owner.IsRole(WildSpawnType.marksman)) || enemy.Owner == null)) return enemy;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            if (enemy != null && !enemy.IsVisible && !enemy.HaveSeen &&
                Vector3.Distance(enemy.CurrPosition, botOwner_0.GetPlayer.Transform.position) <= botOwner_0.Settings.Current.CurrentHearingSense &&
                Utils.Utils.GetNavDistance(botPosition,enemy.CurrPosition) > botOwner_0.Settings.Current.CurrentHearingSense * 2.5f
            )
            {
                enemy = null;
            }

            if(enemy == null && botOwner_0.BotFollower.HaveBoss)
            {
                foreach (var item in botOwner_0.BotFollower.BossToFollow.Followers)
                {
                    if (item.Memory.HaveEnemy)
                    {
                        enemy = item.Memory.GoalEnemy;
                        break;
                    }
                }

                if (enemy == null && BossPlayers.Instance.IsBoss(botOwner_0.BotFollower.BossToFollow.Player().ProfileId))
                {
                    pitAIBossPlayer boss = botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer;
                    if (boss.GetEnemies().Count > 0)
                    {
                        BotOwner bosEnemy = boss.ClosestEnemy();
                        if (bosEnemy != null && !bosEnemy.IsDead && bosEnemy.BotState == EBotState.Active)
                        {
                            enemy = new EnemyInfo(botOwner_0.BotsGroup, bosEnemy, botOwner_0, new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(bosEnemy.ProfileId), botOwner_0.BotsGroup, EBotEnemyCause.addPlayerToBoss));
                        }
                    }
                }
            }*/    

            return enemy;
        }
    }
}
