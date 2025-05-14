using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Core;
using Debug = WxAxW.PinAssistant.Utils.Debug;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal class MinimapPatches
    {
        public static event Action<Minimap.PinData> OnPinAdd;
        public static event Action<Minimap.PinData> OnPinRemove;
        public static event Action OnPinClear;
        public static event Action<Minimap.PinData> OnPinSetTarget;
        public static event Action<Minimap.PinData, Minimap.PinData> OnPinUpdate; // when user changed the pin name/type once more. Doesn't apply to Vanilla
        public static event Action OnMinimapUpdatePins;

        public static bool isSpecialPin = false;
        public static bool isManualPin = false;

        public static Minimap.PinData m_edittingPinInitial = new Minimap.PinData();
        public static Minimap.PinData m_edittingPinCurrent;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.AddPin))]
        private static void Postfix(ref Minimap.PinData __result) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnPinAdd?.Invoke(__result);
            isSpecialPin = false;
            isManualPin = false;
        }

        // patches the specific function RemovePin(PinData pin) inside the Minimap Class
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.RemovePin), new Type[] { typeof(Minimap.PinData) })]
        private static void Prefix(ref Minimap.PinData pin)
        {
            OnPinRemove?.Invoke(pin);
        }

        // patch to clear the plugin's m_monitoredPins as well
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.ClearPins))]
        private static void PrefixClearPins()
        {
            OnPinClear?.Invoke();
        }

        // Always run after Minimap.UpdatePins, to reapply filters and colors on every tick
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.UpdatePins))]
        private static void PostFixUpdatePins()
        {
            OnMinimapUpdatePins?.Invoke();
        }

        // set as new pin
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
        private static void ShowPinNameInputPrefix()
        {
            isManualPin = true;
            Debug.Log("Manual pin added");
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
        private static void PostFixShowPinNameInput(Minimap __instance, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

            SetTargetPin(__instance.m_namePin);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.HidePinTextInput))]
        private static void PrefixHidePinTextInput(Minimap __instance)
        {
            if (__instance.m_namePin == null) return;
            Debug.Log("New manual pin editted");
            OnPinUpdate?.Invoke(m_edittingPinInitial, m_edittingPinCurrent);
            SetTargetPin(null);
        }

        public static void SetTargetPin(Minimap.PinData pin)
        {
            OnPinSetTarget?.Invoke(pin);
            m_edittingPinCurrent = pin;
            CopyValues(m_edittingPinCurrent);
        }

        public static void UpdatePin()
        {
            OnPinUpdate?.Invoke(m_edittingPinInitial, m_edittingPinCurrent);
            CopyValues(m_edittingPinCurrent);
        }

        private static void CopyValues(Minimap.PinData pin)
        {
            if (pin == null) return;
            m_edittingPinInitial.m_name = pin.m_name;
            m_edittingPinInitial.m_type = pin.m_type;
            m_edittingPinInitial.m_pos = pin.m_pos;
        }

        // ignore special pins
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.DiscoverLocation))] // ignore newly discovered pin
        [HarmonyPatch(nameof(Minimap.UpdateProfilePins))] // ignore spawn point pin
        [HarmonyPatch(nameof(Minimap.UpdateEventPin))] // ignore event pin (the red circle and exclamation point) | has 2 add pin but exclude method covers that by continuously repeating till matcher doesn't find anything anymore
        [HarmonyPatch(nameof(Minimap.UpdateLocationPins))] // ignore boss pins, merchant, others
        [HarmonyPatch(nameof(Minimap.UpdatePlayerPins))] // ignore player pins (yourself, and others?)
        [HarmonyPatch(nameof(Minimap.UpdatePingPins))] // ignore ping pins (Ping when middle clicking minimap)
        [HarmonyPatch(nameof(Minimap.UpdateShoutPins))] // ignore shout pins (I have Arrived!)
        private static IEnumerable<CodeInstruction> TranspilerIgnoreNewPin(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludePinsInMethod(instructions);
        }

        public static IEnumerable<CodeInstruction> ExcludePinsInMethod(IEnumerable<CodeInstruction> instructions, bool isVirtual = false)
        {
            // without emit delegate
            return FindCall(instructions, useEnd: false, isVirtual, AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin)))
                .Repeat(
                    matcher =>
                    {
                        matcher.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_1), // add 1 (true) to stack to set "isSpecialPin" to true
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MinimapPatches), nameof(isSpecialPin))) // add variable isSpecialPin
                        )
                        .Advance(1); // advance to after AddPin Method so we don’t re-process it
                    }
                )
                .InstructionEnumeration();
        }

        // Finds a match where it's a call with a specified method and sets the position to before or after that call
        private static CodeMatcher FindCall(IEnumerable<CodeInstruction> instructions, bool useEnd, bool isVirtual, MethodInfo method)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    useEnd,
                    new CodeMatch(isVirtual ? OpCodes.Callvirt : OpCodes.Call, method) // ex. find the op code call/virt, before the "method" (AddPin, in this case)
                    );
        }

        /* Test code to change pin color
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdatePins))] // ignore newly discovered pin
        private static IEnumerable<CodeInstruction> TranspilerChangePinColor(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.white)) // find the instruction that gets Color.white
                    )
                )
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.red)))) // set the instruction to get Color.Red
                .InstructionEnumeration();
        }
        */
    }
}