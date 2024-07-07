using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace friendlyPMC.Actions
{
    internal class FollowerSearch : GClass399
    {

        protected float float_0 = 0f;
        public FollowerSearch([NotNull] BotOwner owner)
        : base(owner)
        {
        }

        public override void UpdateByNode()
        {
            EnemyInfo enemyInfo = botOwner_0.Memory.GoalEnemy;
            if (enemyInfo == null)
            {
                enemyInfo = botOwner_0.Memory.LastEnemy;
            }
            if (enemyInfo == null)
            {
                if (botOwner_0.BotFollower.HaveBoss && BossPlayers.Instance.IsBoss(botOwner_0.BotFollower.BossToFollow.Player().ProfileId))
                {
                    BotOwner enemy = (botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer).ClosestEnemy();
                    if (enemy != null)
                    {
                        botOwner_0.Sprint(true, false);
                        method_7(enemy.GetPlayer.Transform.position);
                        botOwner_0.LookData.SetLookPointByHearing(null);
                        return;
                    }
                }

                if (botOwner_0.Memory.IsInCover)
                {
                    botOwner_0.StopMove();
                    botOwner_0.LookData.SetLookPointByHearing(null);
                    return;
                }
                GotoClosestCover();
                return;
            }
            else
            {
                method_7(enemyInfo.CurrPosition);
                return;
            }
        }

        public void GotoClosestCover()
        {
            if (float_0 < Time.time)
            {
                float_0 = Time.time + 1f;
                CustomNavigationPoint freeClosePoint = Utils.Utils.GetClosestCoverPoint(botOwner_0, botOwner_0.GetPlayer.Transform.position, 100f);
                botOwner_0.GoToPoint(freeClosePoint);
            }
        }
    }
}
