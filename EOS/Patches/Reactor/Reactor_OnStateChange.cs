using ChainedPuzzles;
using EOS.Modules.Instances;
using EOS.Modules.Objectives.Reactor;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch]
    internal class Reactor_OnStateChange // full overwrite        
    {
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_WardenObjective_Reactor_OnStateChange(LG_WardenObjective_Reactor __instance, pReactorState oldState, pReactorState newState, bool isDropinState)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                return true;

            if (__instance.m_isWardenObjective)
            {
                if (ReactorInstanceManager.Current.IsStartupReactor(__instance))
                {
                    Startup_OnStateChange(__instance, oldState, newState, isDropinState);
                }
                return true; // also use vanilla impl
            }

            if (oldState.stateCount != newState.stateCount)
                __instance.OnStateCountUpdate(newState.stateCount);
            if (oldState.stateProgress != newState.stateProgress)
                __instance.OnStateProgressUpdate(newState.stateProgress);
            if (oldState.status == newState.status)
                return false;

            __instance.ReadyForVerification = false;

            if (ReactorInstanceManager.Current.IsShutdownReactor(__instance))
            {
                if (!ReactorShutdownObjectiveManager.Current.TryGetDefinition(__instance, out var def))
                {
                    EOSLogger.Error("Reactor_OnStateChange: found built custom reactor but its definition is missing, what happened?");
                    return false;
                }
                Shutdown_OnStateChange(__instance, oldState, newState, isDropinState, def);
            }
            else
            {
                EOSLogger.Error("Reactor_OnStateChange: found built custom reactor but it's not a shutdown reactor, what happened?");
                return false;
            }

            __instance.m_currentState = newState;
            return false;
        }

        private static void Startup_OnStateChange(LG_WardenObjective_Reactor reactor, pReactorState oldState, pReactorState newState, bool isDropinState)
        {
            if (isDropinState || !ReactorStartupOverrideManager.Current.TryGetDefinition(reactor, out var def))
                return;

            // NOTE: eReactorStatus.Active_Idle is for shutdown
            if (oldState.status == eReactorStatus.Inactive_Idle && reactor.m_chainedPuzzleToStartSequence != null)
            {
                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnActive);
            }
        }

        private static void Shutdown_OnStateChange(LG_WardenObjective_Reactor reactor, pReactorState oldState, pReactorState newState, bool isDropinState, ReactorShutdownDefinition def)
        {
            switch (newState.status)
            {
                case eReactorStatus.Shutdown_intro:
                    GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1080U));
                    reactor.m_progressUpdateEnabled = true;
                    reactor.m_currentDuration = 15f;
                    reactor.m_lightCollection.SetMode(false);
                    reactor.m_sound.Stop();
                    EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnActive);
                    break;

                case eReactorStatus.Shutdown_waitForVerify:
                    GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1081U));
                    reactor.m_progressUpdateEnabled = false;
                    reactor.ReadyForVerification = true;
                    break;

                case eReactorStatus.Shutdown_puzzleChaos:
                    reactor.m_progressUpdateEnabled = false;
                    if (def.ChainedPuzzleOnVerificationInstance != null)
                    {
                        GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1082U));
                        def.ChainedPuzzleOnVerificationInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }
                    EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnShutdownPuzzleStarts);
                    break;

                case eReactorStatus.Shutdown_complete:
                    reactor.m_progressUpdateEnabled = false;
                    reactor.m_objectiveCompleteTimer = Clock.Time + 5f;
                    EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnComplete);
                    break;
            }
        }
    }
}
