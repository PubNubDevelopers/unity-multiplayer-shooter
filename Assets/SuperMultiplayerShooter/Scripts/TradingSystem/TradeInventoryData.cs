using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct TradeInventoryData : IJsonSerializable
    {
        [SerializeField] private List<int> hats;

        public TradeInventoryData(List<int> cosmeticItems)
        {
            this.hats = cosmeticItems;
        }

        public List<int> CosmeticItems { get => hats; set => hats = value; }
    }
}
