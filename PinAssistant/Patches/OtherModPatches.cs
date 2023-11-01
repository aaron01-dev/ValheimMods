using HarmonyLib;
using Pinnacle;
using Kits_Bitz.Under_The_Radar;
using System;
using System.Collections.Generic;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(PinEditPanel))]
    internal class PinnaclePatches
    {
        public static event Action<Minimap.PinData> OnSetTargetPin;

        public static event Action OnPinNameChanged;

        // pin is either being created and editted or being selected and editted
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.SetTargetPin))]
        private static void PostfixSetTargetPin(ref Minimap.PinData pin) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnSetTargetPin?.Invoke(pin);
        }

        // pin has finished being editted
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.OnPinNameValueChange))]
        private static void PostfixOnPinNameValueChange()
        {
            OnPinNameChanged?.Invoke();
        }
    }

    [HarmonyPatch(typeof(RadarPinComponent))]
    internal class UnderTheRadarPatches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(RadarPinComponent.Update))] // ignore shout pins (I have Arrived!)
        private static IEnumerable<CodeInstruction> TranspilerIgnoreRadarPin(IEnumerable<CodeInstruction> instructions)
        {
            return MinimapPatches.ExcludePinsInMethod(instructions, isVirtual: true);
        }
    }
}