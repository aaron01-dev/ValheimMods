using HarmonyLib;
using System;
using WxAxW.PinAssistant.Components;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal class MinimapPatch
    {
        public delegate void OnPinAddHandler(Minimap.PinData pinData);
        public static event OnPinAddHandler OnPinAdd;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.AddPin))]
        private static void Postfix(ref Minimap.PinData __result) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnPinAdd?.Invoke(__result);
        }

        // patches the specific function RemovePin(PinData pin) inside the Minimap Class
        public delegate void OnRemovePinHandler(Minimap.PinData pinData);
        public static event OnRemovePinHandler OnPinRemove;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.RemovePin), new Type[] { typeof(Minimap.PinData) })]
        private static void Prefix(ref Minimap.PinData pin)
        {
            OnPinRemove?.Invoke(pin);
        }

        // patch to clear the plugin's m_pins as well
        public delegate void OnPinClearHandler();
        public static event OnPinClearHandler OnPinClear;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.ClearPins))]
        private static void Prefix()
        {
            OnPinClear?.Invoke();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.UpdatePins))]
        private static void PostFix()
        {
            FilterPinsUI.Instance?.FilterPins();
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
}