using EFT;
using friendlyPMC.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Modules
{
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
            
            return GClass460.CreateNode(type, bot);
        }
    }
}
