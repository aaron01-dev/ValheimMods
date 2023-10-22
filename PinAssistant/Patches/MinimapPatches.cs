using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using WxAxW.PinAssistant.Components;
using System.Reflection.Emit;
using System.Reflection;
using Mono.Cecil.Cil;
using OpCodes = System.Reflection.Emit.OpCodes;
using WxAxW.PinAssistant.Core;
using static WxAxW.PinAssistant.Patches.MinimapPatches;
using WxAxW.PinAssistant.Configuration;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal class MinimapPatches
    {
        public static event Action<Minimap.PinData> OnPinAdd;
        public static event Action<Minimap.PinData> OnPinRemove;
        public static event Action<Minimap.PinData> OnSpecialPinRemove;
        public static event Action OnPinClear;
        public static event Action<Minimap.PinData> OnPinSetup;
        public static event Action OnPinNameChanged;

        private static FieldInfo AccessMinimapField(string field)
        {
            return AccessTools.Field(typeof(Minimap), field);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.AddPin))]
        private static void Postfix(ref Minimap.PinData __result) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnPinAdd?.Invoke(__result);
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

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
        private static void PostFixShowPinNameInput(Minimap __instance, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

            Debug.Log("New manual pin added");
            OnPinSetup?.Invoke(__instance.m_namePin);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.HidePinTextInput))]
        private static void PrefixHidePinTextInput(Minimap __instance)
        {
            if (__instance.m_namePin == null) return;
            Debug.Log("New manual pin editted");
            OnPinNameChanged?.Invoke();
        }

        // ignore special pins

        private static Minimap.PinData InvokeRemovePin(Minimap.PinData pin)
        {
            OnSpecialPinRemove?.Invoke(pin);
            return pin;
        }

        // ignore newly discovered pin
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.DiscoverLocation))]
        private static IEnumerable<CodeInstruction> TranspilerDiscoverLocation(IEnumerable<CodeInstruction> instructions)
        {
            return FindAddPin(instructions)
                .Advance(offset: 1) // advance 1 position in the stack (opcode pop -> where the return value of AddPin was popped or removed)
                .SetInstructionAndAdvance(new CodeInstruction(Transpilers.EmitDelegate<Func<Minimap.PinData, Minimap.PinData>>(InvokeRemovePin))) // invoke OnPinAdd
                .InsertAndAdvance(new CodeInstruction(OpCodes.Pop))   // remove return value of InvokeRemovePin
                .InstructionEnumeration();
        }

        // ignore spawn point pin
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdateProfilePins))]
        private static IEnumerable<CodeInstruction> TranspilerUpdateProfilePin(IEnumerable<CodeInstruction> instructions)
        {
            // using emit delegate
            return FindAddPin(instructions)
                .Advance(offset: 2) // move to after setting AddPin return value to variable
                .InsertAndAdvance(
                    ExcludeFieldInMethod(nameof(Minimap.m_spawnPointPin))
                )
                .InstructionEnumeration();

            /*
            CodeInstruction startInvokePinRemove = new CodeInstruction(OpCodes.Ldarg_0);
            Label startInvokeLabel = il.DefineLabel();
            Label endInvokeLabel = il.DefineLabel();

            startInvokePinRemove.labels.Add(startInvokeLabel);

            return new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin))))
                // stfld here this.m_spawnPointPin = AddPin()
                .ThrowIfInvalid("Couldn't find AddPin Method")
                .Advance(offset: 2) // move to after setting m_spawnPointPin with AddPin return value (m_spawnPointPin = AddPin())
                .AddLabels(new[] { endInvokeLabel }) // add label for when the invoke is null (no listeners subscribed to it)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldsfld, OnPinRemoveField),
                    new CodeInstruction(OpCodes.Dup),   // duplicate field OnPinRemove
                    new CodeInstruction(OpCodes.Brtrue_S, startInvokeLabel), // consume loaded invoke value and go to label if true

                    new CodeInstruction(OpCodes.Pop), // if Invoke is null pop the duplicated invoke value in the stack
                    new CodeInstruction(OpCodes.Br_S, endInvokeLabel),   // skip to after invoke pin remove. (go to original code after m_spawnPoint = AddPin())

                    new CodeInstruction(startInvokePinRemove),   // load this instance's (startInvokeLabel is added to this instruction)
                    new CodeInstruction(OpCodes.Ldfld, AccessMinimapField(nameof(Minimap.m_spawnPointPin))),    // m_spawnPointPin
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(OnPinHandler), nameof(OnPinHandler.Invoke))) // finally, invoke pin remove
                )
                .InstructionEnumeration();
            */
        }

        // ignore event pin (the red circle and exclamation point)
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdateEventPin))]
        private static IEnumerable<CodeInstruction> TranspilerUpdateEventPin(IEnumerable<CodeInstruction> instructions)
        {
            int index = 0;
            string[] pins = new string[] { nameof(Minimap.m_randEventAreaPin), nameof(Minimap.m_randEventPin) }; // pin field to invoke to remove from list
            return FindAddPin(instructions)
                .Repeat( // repeat 2 times cause the method creates 2 pins
                    matcher => {
                        matcher.Advance(offset: 2); // move to after setting AddPin return value to variable
                        if (index >= pins.Length) return; // if it goes over cause instruction found another AddPin (probably due to an update, skip it)

                        matcher
                            .InsertAndAdvance(
                                ExcludeFieldInMethod(pins[index])
                            );
                        index++;
                    }
                )
                .InstructionEnumeration();
        }

        // ignore boss pins, merchant, others
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdateLocationPins))]
        private static IEnumerable<CodeInstruction> TranspilerUpdateLocationPins(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludeLocalPinInMethod(instructions, pinDataIndex: 7);
        }

        // ignore player pins (yourself, and others?)
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdatePlayerPins))]
        private static IEnumerable<CodeInstruction> TranspilerUpdatePlayerPins(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludeLocalPinInMethod(instructions, pinDataIndex: 3);
        }

        // ignore ping pins (Ping when middle clicking minimap)
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdatePingPins))]
        private static IEnumerable<CodeInstruction> TranspilerUpdatePingPins(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludeLocalPinInMethod(instructions, pinDataIndex: 4);
        }

        // ignore shout pins (I have Arrived!)
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.UpdateShoutPins))]
        private static IEnumerable<CodeInstruction> TranspilerUpdateShoutPins(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludeLocalPinInMethod(instructions, pinDataIndex: 4);
        }

        private static CodeInstruction[] ExcludeFieldInMethod(string pinField)
        {
            return new[] {
                new CodeInstruction(OpCodes.Ldarg_0),   // load this instance's
                new CodeInstruction(OpCodes.Ldfld, AccessMinimapField(pinField)),    // m_spawnPointPin
                new CodeInstruction(Transpilers.EmitDelegate<Func<Minimap.PinData, Minimap.PinData>>(InvokeRemovePin)),
                new CodeInstruction(OpCodes.Pop)    // remove the return value of InvokeRemovePin
            };
        }

        private static IEnumerable<CodeInstruction> ExcludeLocalPinInMethod(IEnumerable<CodeInstruction> instructions, int pinDataIndex)
        {
            return FindAddPin(instructions)
                .Advance(offset: 2) // move to after setting AddPin return value to local variable ( PinData pinData = AddPin() )
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, pinDataIndex),   // load PinData pinData to stack
                    new CodeInstruction(Transpilers.EmitDelegate<Func<Minimap.PinData, Minimap.PinData>>(InvokeRemovePin)),
                    new CodeInstruction(OpCodes.Pop)    // remove the return value of InvokeRemovePin
                )
                .InstructionEnumeration();
        }

        private static CodeMatcher FindAddPin(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin)))
                    )
                .ThrowIfInvalid("Couldn't find AddPin Method");
        }
    }
}
/*
[HarmonyPatch(typeof(Minimap), "ResetSharedMapData")]
internal static class ResetSharedMapData_Patch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;

            // Look for the line that calls m_pins.RemoveAt(num)
            if (instruction.opcode == OpCodes.Callvirt &&
                instruction.operand is MethodInfo methodInfo &&
                methodInfo.Name == "RemoveAt" &&
                methodInfo.DeclaringType == typeof(List<Minimap.PinData>))
            {
                // Insert your custom code right after m_pins.RemoveAt(num)
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(AutoPinPlugin), "Instance")); // Load MyClass.instance
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AutoPinPlugin), "m_pins")); // Load m_pins from MyClass.instance
                yield return new CodeInstruction(OpCodes.Ldarg_0); // Load the current instance of YourClass
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Minimap.PinData), "m_pos")); // Load m_pos from the current PinData
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Vector3>), "Remove")); // Call m_pins.Remove(m_pins[num].m_pos)
            }
            /*
            public void ResetSharedMapData()
            {
                Color[] pixels = m_fogTexture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].g = 255f;
                }
                m_fogTexture.SetPixels(pixels);
                m_fogTexture.Apply();
                for (int j = 0; j < m_exploredOthers.Length; j++)
                {
                    m_exploredOthers[j] = false;
                }
                for (int num = m_pins.Count - 1; num >= 0; num--)
                {
                    PinData pinData = m_pins[num];
                    if (pinData.m_ownerID != 0L)
                    {
                        DestroyPinMarker(pinData);
                        m_pins.RemoveAt(num);
                        AutoPinPlugin.Instance.m_pins.Remove(m_pins.m_pos) // add code here
                    }
                }
                m_sharedMapHint.gameObject.SetActive(value: false);
            }
            */