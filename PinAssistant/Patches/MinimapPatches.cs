using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        public static event Action OnPinUpdate;

        public static bool isSpecialPin = false;
        public static bool isManualPin = false;

        public static Minimap.PinData m_edittingPinInitial = new Minimap.PinData();
        public static Minimap.PinData m_edittingPin;

        private static FieldInfo AccessMinimapField(string field)
        {
            return AccessTools.Field(typeof(Minimap), field);
        }

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

        // patch to clear the plugin's m_pins as well
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.ClearPins))]
        private static void PrefixClearPins()
        {
            OnPinClear?.Invoke();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.UpdatePins))]
        private static void PostFixUpdatePins()
        {
            if (!ModConfig.Instance.IsEnabledConfig.Value) return;
            MinimapAssistant.Instance.FilterPins();
            MinimapAssistant.Instance.ColorPins();
        }

        // set as new pin
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdateMap))]
        [HarmonyPatch(nameof(Minimap.OnMapDblClick))]
        private static IEnumerable<CodeInstruction> TranspilerIsManualPin(IEnumerable<CodeInstruction> instructions)
        {
            return FindBeforeCall(instructions, isVirtual: false, AccessTools.Method(typeof(Minimap), nameof(Minimap.ShowPinNameInput)))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_1), // add 1 (true) to stack
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MinimapPatches), nameof(isManualPin)))
                )
                .InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
        private static void PostFixShowPinNameInput(Minimap __instance, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

            Debug.Log("New manual pin added");
            SetTargetPin(__instance.m_namePin);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.HidePinTextInput))]
        private static void PrefixHidePinTextInput(Minimap __instance)
        {
            if (__instance.m_namePin == null) return;
            Debug.Log("New manual pin editted");
            OnPinUpdate?.Invoke();
            SetTargetPin(null);
        }

        public static void SetTargetPin(Minimap.PinData pin)
        {
            OnPinSetTarget?.Invoke(pin);
            m_edittingPin = pin;
            CopyValues(m_edittingPin);
        }

        public static void UpdatePin()
        {
            OnPinUpdate?.Invoke();
            CopyValues(m_edittingPin);
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
            /* with emit delegate
            return FindBeforeAddPin(instructions)
                .Insert(
                    new CodeInstruction(Transpilers.EmitDelegate<Func<bool>>(SetIsSpecialPin)),
                    new CodeInstruction(OpCodes.Pop)    // remove the return value of SetIsSpecialPin
                )
                .InstructionEnumeration();
            */
            // without emit delegate
            return FindBeforeCall(instructions, isVirtual, AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin)))
                .Repeat(
                    matcher =>
                    {
                        matcher.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_1), // add 1 (true) to stack
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MinimapPatches), nameof(isSpecialPin)))
                        )
                        .Advance(1); // advance to after AddPin Method
                    }
                )
                .InstructionEnumeration();
        }

        private static CodeMatcher FindBeforeCall(IEnumerable<CodeInstruction> instructions, bool isVirtual, MethodInfo method)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(isVirtual ? OpCodes.Callvirt : OpCodes.Call, method)
                    );
        }
    }
}