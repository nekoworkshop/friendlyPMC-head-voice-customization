using friendlyPMC.Components;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Modules
{
    internal class Receivers
    {
        private static Dictionary<string, FollowerReceiver> followerReceivers;

        private static Receivers Instance;

        public Receivers()
        {
            if (Instance == null)
            {
                if (followerReceivers == null)
                {
                    followerReceivers = new Dictionary<string, FollowerReceiver>();
                }
                Instance = this;
            }
        }
        
        public void Destroy()
        {
            foreach (var item in followerReceivers)
            {
                item.Value.Dispose();
            }
            followerReceivers.Clear();
        }

        public static void Dispose()
        {
            if(Instance != null)
            {
                Instance.Destroy();
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
