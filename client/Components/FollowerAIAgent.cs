using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class FollowerAIAgent<T> : AICoreAgentClass<T>
    {
        public FollowerAIAgent(AICoreControllerClass aiCoreController, AICoreStrategyAbstractClass<T> strategy, Dictionary<T, GClass134> nodesDictionary, GameObject monoBehObject, string name, Func<T, GClass134> lazyGetter) : base(aiCoreController, strategy, nodesDictionary, monoBehObject, name, lazyGetter)
        {

        }

        public event Action<AICoreActionResultStruct<T>> OnUpdate;
        public event EventHandler<EventArgs> OnDispose;
        public override void Update()
        {

            try
            {
                base.Update();
                AICoreActionResultStruct<T>? actionResultStruct = base.LastResult();

                if (actionResultStruct.HasValue) OnUpdate?.Invoke(actionResultStruct.Value);

            }
            catch (Exception ex)
            {
                Logger.LogError("AIAgent Error");
                Logger.LogError(ex);
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            OnDispose?.Invoke(this, EventArgs.Empty);
        }
    }
}
