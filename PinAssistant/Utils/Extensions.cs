using System.Collections.Generic;

namespace WxAxW.PinAssistant.Utils
{
    internal static class DictionaryExtensions
    {
        public static bool ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                           TKey oldKey, TKey newKey)
        {
            TValue value;
            if (!dict.TryGetValue(oldKey, out value))
                return false;

            dict.Remove(oldKey);
            dict.Add(newKey, value);
            return true;
        }
    }
}