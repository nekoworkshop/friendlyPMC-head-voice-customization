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

        private static readonly FieldInfo aICoreActionResultStructField = AccessTools.Field(typeof(AICoreAgentClass<T>), "aICoreActionResultStruct");
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
                AICoreActionResultStruct<T> actionResultStruct = (AICoreActionResultStruct<T>)aICoreActionResultStructField.GetValue(this);
                // Use the actionResultStruct as needed
                OnUpdate?.Invoke(actionResultStruct);
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("AIAgent Error " + ex.Message);
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            OnDispose?.Invoke(this, EventArgs.Empty);
        }
    }
}
