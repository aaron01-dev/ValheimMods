extern alias HUDCompassAlias; // This is needed to avoid a conflict with Jotunn's libs
using HarmonyLib;
using Pinnacle;
using Kits_Bitz.Under_The_Radar;
using System;
using System.Collections.Generic;
using Debug = WxAxW.PinAssistant.Utils.Debug;
using HUDCompassAlias::neobotics.ValheimMods;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine.UI;
using UnityEngine;
using WxAxW.PinAssistant.Integration;

namespace WxAxW.PinAssistant.Patches
{
    // Compatibility patch to work with Pinnacles edit feature.
    [HarmonyPatch(typeof(PinEditPanel))]
    internal class PinnaclePatches
    {
        // pin is either being created and editted or being selected and editted
        // Pinnacle overrides adding pins so the PostFixShowPinNameInput patch in MinimapPatches is completely/skipped
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

    [HarmonyPatch(typeof(HUDCompass.HudUpdateCompassPatch))]
    public static class HUDCompassPatches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(HUDCompass.HudUpdateCompassPatch.Prefix))]
        // This runs *after* the original patch’s Prefix
        private static IEnumerable<CodeInstruction> TranspilerPrefix(IEnumerable<CodeInstruction> instructions)
        {
            // location of item2 or current pin data
            /*
             * // foreach (Minimap.PinData item2 in list2)
		     * IL_044b: br IL_07f8
		     * // loop start (head: IL_07f8)
			 * IL_0450: ldloca.s 15
			 * IL_0452: call instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class [assembly_valheim]Minimap/PinData>::get_Current()
			 * IL_0457: stloc.s 16 // <-- item2
            */


            /*
             * // image.color = Cfg.colorPins.Value;
			 * IL_06a4: ldloc.s 25
			 * Matches here, then remove -> IL_06a6: ldsfld class [BepInEx]BepInEx.Configuration.ConfigEntry`1<valuetype [UnityEngine.CoreModule]UnityEngine.Color> neobotics.ValheimMods.Cfg::colorPins
			 * ends up here, then replace -> IL_06ab: callvirt instance !0 class [BepInEx]BepInEx.Configuration.ConfigEntry`1<valuetype [UnityEngine.CoreModule]UnityEngine.Color>::get_Value()
			 * IL_06b0: callvirt instance void [UnityEngine.UI]UnityEngine.UI.Graphic::set_color(valuetype [UnityEngine.CoreModule]UnityEngine.Color)
            */

            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Cfg), nameof(Cfg.colorPins)))
                )
                .RemoveInstructions(2)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, (byte)16), // load the local variable (item2) to the stack
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HUDCompassHandler), nameof(HUDCompassHandler.GetPinColor))) // call the method to get the color
                    //new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Graphic), nameof(Graphic.color))) // set loc 25's (image) color property to the color of whatever the return value of "ApplyColor" is.
                )
                .InstructionEnumeration();

            /* test transpiler
            return new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Cfg), nameof(Cfg.colorPins)))
                )
                .RemoveInstructions(1)
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.red)))) // set the instruction to get Color.Red
                .InstructionEnumeration();
            */
        }
    }
}