using UnityEngine;
using static UnityEngine.RectTransform;

namespace WxAxW.PinAssistant.Utils
{
    internal static class GUIExtension
    {
        // taken from jotunn gui extension
        internal static GameObject SetSize(this GameObject go, float width, float height)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors((Axis)0, width);
            rect.SetSizeWithCurrentAnchors((Axis)1, height);
            return go;
        }
    }
}