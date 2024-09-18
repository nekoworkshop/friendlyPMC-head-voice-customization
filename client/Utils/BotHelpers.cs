using EFT;
using friendlyPMC.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Utils
{
    internal class BotHelpers : MonoBehaviour
    {
        private Coroutine watchCoroutine;

        private BotOwner botOwner;
        public void AttachStuckWatcher(BotOwner bot)
        {

            FollowerBrain brain = bot.Brain.BaseBrain as FollowerBrain;
            brain.OnDispose += DetachStuckWatcher;

            botOwner = bot;

            StartCoroutine(UpdateWatchCoroutine());

            botOwner.GetPlayer.HealthController.DiedEvent += OnDead;
            botOwner.LeaveData.OnLeave += OnLeave;
        }

        private void DetachStuckWatcher(BotOwner bot)
        {
            FollowerBrain brain = bot.Brain.BaseBrain as FollowerBrain;
            brain.OnDispose -= DetachStuckWatcher;
            StopCoroutine(UpdateWatchCoroutine());
            botOwner.GetPlayer.HealthController.DiedEvent -= OnDead;
            botOwner.LeaveData.OnLeave -= OnLeave;
        }

        private void OnDead(EDamageType damageType)
        {
            DetachStuckWatcher(botOwner);

        }
        private void OnLeave(BotOwner _bot)
        {
            DetachStuckWatcher(botOwner);
        }
        private IEnumerator UpdateWatchCoroutine()
        {
            while (true)
            {
                if(botOwner.BotState != EBotState.Active || botOwner.IsDead) yield break;
                yield return new WaitForSeconds(2f);
            }
        }

        private bool isBotMoving()
        {
            bool isMoving = false;
            isMoving = botOwner.Mover.IsMoving || botOwner.Mover.Sprinting;

            return isMoving;
        }
    }
}
