using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.Tactics
{
    /** This class is not meant to be used directly as a brain layer, but within one **/
    internal class FollowerHolderLayer : GClass47
    {
        private FollowerCommonLayer commonLayer;

        public FollowerCommonLayer CommonLayer { get { return commonLayer; } }

        private bool existingCommon = false;

        public bool onlyAssist = false;

        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        public FollowerHolderLayer(BotOwner bot, int priority, FollowerCommonLayer commonLayer = null) : base(bot, priority)
        {
            if (commonLayer != null)
            {
                this.commonLayer = commonLayer;
                existingCommon = true;
            }
            else this.commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            if(!existingCommon) commonLayer?.OnActivate();
        }
        public override void Dispose()
        {
            base.Dispose();
            if (!existingCommon) commonLayer?.Dispose();
        }
        public void OrdersChanged()
        {
            commonLayer.OrdersChanged();
        }

        public bool ShallGoNearBoss()
        {
            return commonLayer.ShallGoNearBoss();
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            commonLayer.DecisionChanged(prevDecision, nextDecision);
            base.DecisionChanged(prevDecision, nextDecision);
        }

        public AICoreActionResultStruct<BotLogicDecision> DefendPosition(Vector3 interestPosition)
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;

            // If the bot is already in cover
            if (botOwner_0.Memory.IsInCover)
            {
                // If the enemy is visible and can be shot, shoot from cover
                if (enemyVisible && (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy || botOwner_0.Memory.GoalEnemy.CanShoot))
                {
                    if (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    else
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");

                }
                // Else check if the bot needs to get close to the boss
                if (ShallGoNearBoss())
                {
                    customNavigationPoint_0 = commonLayer.GetClosestCoverPointGroup(interestPosition, commonLayer.coverSearchRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < commonLayer.sprintDistance)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
                }
                // Otherwise, hold position
                return commonLayer.HoldPositionFor(GClass761.Random(2f, 3f), "holdPositionInCover");
            }

            // If the bot is not in cover, find the closest cover and move to it
            if (commonLayer.HasBoss() && commonLayer.coverType == "close")
            {
                customNavigationPoint_0 = commonLayer.GetClosestCoverPointGroup(interestPosition, commonLayer.coverSearchRadius);

                if (customNavigationPoint_0 != null)
                {
                    if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < commonLayer.sprintDistance)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");
                    else
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                }

                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
            }
            else
            {
                customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(interestPosition, commonLayer.coverSearchRadius);
            }

            if (customNavigationPoint_0 != null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveToCover");
            }

            // fallback decision if no cover is found
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            try
            {
                if (commonLayer.OrderHasChangedRecently)
                {
                    return new AICoreActionEndStruct("EndHol", true);
                }


                AIBossPlayerLogic gclass363_0 = commonLayer.HasBoss() ? commonLayer.GetBoss().GetBossLogic() : null;

                string text;
                if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.Person.HealthController.IsAlive && base.method_5(out text))
                {
                    return new AICoreActionEndStruct("cst", true);
                }

                if (commonLayer.TimeToHeal())
                {
                    return new AICoreActionEndStruct("wntHeal", true);
                }

                if (this.customNavigationPoint_0 != null && !this.customNavigationPoint_0.IsFreeById(this.botOwner_0.Id))
                {
                    this.customNavigationPoint_0 = null;
                }


                if (ShallGoNearBoss()) return new AICoreActionEndStruct("goNearBoss", true);

                if (base.method_6())
                {
                    return new AICoreActionEndStruct("EndHol", true);
                }
                if (!this.botOwner_0.Memory.IsInCover)
                {
                    return new AICoreActionEndStruct("notInCover", true);
                }
                EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
                if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    return new AICoreActionEndStruct("CanShoot", true);
                }
                if (gclass363_0.IsHitted && commonLayer.coverType == "close")
                {
                    return new AICoreActionEndStruct("bossHit", true);
                }

                return aICoreActionEndStruct_1;
            }
            catch (Exception e)
            {
                Logger.LogError("EndHoldPosition Error");
                Logger.LogError(e);
                return new AICoreActionEndStruct("hpError", true);
            }
        }
    }
}
