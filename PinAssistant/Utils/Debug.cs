using WxAxW.PinAssistant.Configuration;
using Text = WxAxW.PinAssistant.Configuration.TextAttribute;

namespace WxAxW.PinAssistant.Utils
{
    internal static class Debug
    {
        public static void Log(object message)
        {
            if (ModConfig.Instance.IsDebugModeConfig.Value) Jotunn.Logger.LogInfo(message);
        }

        public static string Log(TextType messageType, params object[] parameters)
        {
            string message = Text.Get(messageType, parameters);
            if (ModConfig.Instance.IsDebugModeConfig.Value) Jotunn.Logger.LogInfo(message);
            return message;
        }
    }
}