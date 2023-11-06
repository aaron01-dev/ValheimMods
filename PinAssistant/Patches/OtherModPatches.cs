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
        // pin is either being created and editted or being selected and editted
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.SetTargetPin))]
        private static void PostfixSetTargetPin(ref Minimap.PinData pin) // get the return value of AddPin to add to plugin's pins dictionary
        {
            MinimapPatches.SetTargetPin(pin);
        }

        // pin has finished being editted
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.OnPinNameValueChange))]
        [HarmonyPatch(nameof(PinEditPanel.OnPinTypeValueChange))]
        private static void PostfixOnPinUpdate()
        {
            MinimapPatches.UpdatePin();
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