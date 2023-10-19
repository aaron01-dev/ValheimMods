using HarmonyLib;
using Pinnacle;
using System;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(PinEditPanel))]
    internal class PinnaclePatches
    {
        public static event Action<Minimap.PinData> OnSetTargetPin;

        public static event Action OnPinNameChanged;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.SetTargetPin))]
        private static void PostfixSetTargetPin(ref Minimap.PinData pin) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnSetTargetPin?.Invoke(pin);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PinEditPanel.OnPinNameValueChange))]
        private static void PostfixOnPinNameValueChange()
        {
            OnPinNameChanged?.Invoke();
        }
    }
}