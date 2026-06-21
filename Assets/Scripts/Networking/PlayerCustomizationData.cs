using System;
using UnityEngine;

namespace MineArena.Networking
{
    [Serializable]
    public class PlayerCustomizationData
    {
        public string weaponId = "default_pickaxe";
        public string skinId = "default";
        public string armorId = "none";
        public string[] cosmeticItems = Array.Empty<string>();
        public NetworkKeyValue[] customFields = Array.Empty<NetworkKeyValue>();

        public static PlayerCustomizationData Default()
        {
            return new PlayerCustomizationData();
        }
    }

    [Serializable]
    public struct NetworkKeyValue
    {
        public string key;
        public string value;
    }
}
