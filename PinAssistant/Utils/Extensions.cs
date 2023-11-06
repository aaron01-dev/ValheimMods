using System.Collections.Generic;
using TMPro;

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

    internal static class TMPDropdownExtensions
    {
        public static void AddOptionWithList<T>(this TMP_Dropdown dropDown, string optionName, List<T> list, T value)
        {
            dropDown.options.Add(new TMP_Dropdown.OptionData(optionName));
            list.Add(value);
        }
    }
}