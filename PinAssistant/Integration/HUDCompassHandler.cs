extern alias HUDCompassAlias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WxAxW.PinAssistant.Core;
using HUDCompassAlias::neobotics.ValheimMods;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Integration
{
    internal class HUDCompassHandler
    {
        public static Color GetPinColor(Minimap.PinData pin)
        {
            Color pinColor = PinGroupHandler.Instance.GetColor(pin);
            if (pinColor == Color.white) return Cfg.colorPins.Value; // fallback to HUDCompass' config
            else return pinColor; // return the color from PinAssistant
        }
    }
}
