using friendlyPMC.Components;
using System.Collections.Generic;

namespace friendlyPMC.Modules
{
    internal class Receivers
    {
        private static Dictionary<string, FollowerReceiver> followerReceivers;

        public Receivers()
        {
            if (followerReceivers == null)
            {
                followerReceivers = new Dictionary<string, FollowerReceiver>();
            }
        }
        public static void AddReceiver(string id, FollowerReceiver receiver)
        {
            if(followerReceivers.ContainsKey(id))
            {
                if (followerReceivers.TryGetValue(id, out receiver))
                {
                    receiver.Destroy();
                }

                followerReceivers.Remove(id);
            }
            followerReceivers.Add(id, receiver);
        }

        public static void RemoveReceiver(FollowerReceiver receiver)
        {
            foreach (var item in followerReceivers)
            {
                if(item.Value == receiver)
                {
                    followerReceivers.Remove(item.Key);
                    break;
                }
            }
        }

        public static FollowerReceiver GetReceiver(string id)
        {
            if (followerReceivers.TryGetValue(id, out FollowerReceiver receiver)) return receiver;
            return null;
        }
    }
}
