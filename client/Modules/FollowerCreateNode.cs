using EFT;
using friendlyPMC.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Modules
{
    // replication of GClass460
    internal class FollowerCreateNode
    {

        public static GClass134 CreateNode(BotLogicDecision type, BotOwner bot)
        {

            if (type == BotLogicDecision.doorOpen)
            {
                return new FollowerDoorOpener(bot, bot.BotRequestController.CurRequest as GClass509);
            }

            if (type == BotLogicDecision.botTakeItem)
            {
                return new FollowerTakeLoot(bot);
            }

            if(type == BotLogicDecision.attackMoving)
                 return new FollowerAttackMove(bot);

            if(type == BotLogicDecision.holdPosition) 
                return new FollowerHoldPosition(bot);
            
            if (type == BotLogicDecision.goToPoint)
                return new FollowerGoToPoint(bot);

            return GClass460.CreateNode(type, bot);
        }

        public static Dictionary<BotLogicDecision, GClass134> ActionsList(BotOwner bot)
        {
            Dictionary<BotLogicDecision, GClass134> dictionary = new Dictionary<BotLogicDecision, GClass134>();
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.holdPosition, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.goToCoverPoint, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.attackMoving, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.attackMovingWithSuppress, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.shootFromPlace, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.simplePatrol, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.followerPatrol, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.lay, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.crawl, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.teleportToCover, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runToCover, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.goToEnemy, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runToEnemy, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runToStationary, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.suppressStationary, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.shootFromStationary, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.dogFight, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.search, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.shootFromCover, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runAwayGrenade, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runAwayBTR, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runToEnemyZigZag, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.shootToSmoke, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.suppressFire, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.followPlayer, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.heal, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.repairMalfunction, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.goToPoint, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.axeTarget, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.oneMeleeAttack, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.grenadeSuicide, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.warnPlayer, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.doorOpen, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.panicSitting, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.healStimulators, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.healAnotherTarget, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.deadBody, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.friendlyTilt, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.eatDrink, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.watchSecondWeapon, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.gesture, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.peaceful, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.followMeRequest, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.peaceHardAim, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.peaceLook, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.suppressGrenade, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runAndThrowGrenadeFromPlace, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.throwGrenadeFromPlace, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.alternativePatrol, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.botDropItem, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.botTakeItem, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.flashed, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.standBy, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.turnAwayLight, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.leaveMap, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.runToCoverZigZag, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.summon, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.khorovodChristmasEvent, bot);
            FollowerCreateNode.smethod_0(dictionary, BotLogicDecision.doGiftChristmasEvent, bot);
            return dictionary;
        }

        public static void smethod_0(Dictionary<BotLogicDecision, GClass134> dictionary, BotLogicDecision botLogicDecision, BotOwner bot)
        {
            dictionary.Add(botLogicDecision, FollowerCreateNode.CreateNode(botLogicDecision, bot));
        }
    }
}
