using Newtonsoft.Json;
using UnityEngine;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct TraderData : IJsonSerializable
    {
        [SerializeField] private string userID;
        [SerializeField] private string nickname;
        [SerializeField] private int playerAvatarType;
        [SerializeField] private int equippedCosmetic;
        [SerializeField] private TradeInventoryData tradeableItems;

        public TraderData(string userID, string nickname, int playerAvatarType, TradeInventoryData tradableItems, int equippedHat)
        {
            this.userID = userID;
            this.nickname = nickname;
            this.playerAvatarType = playerAvatarType;
            this.tradeableItems = tradableItems;
            this.equippedCosmetic = equippedHat;
        }

        /// <summary>
        /// PubNub ID
        /// </summary>
        [JsonProperty("pnuser")]
        public string UserID { get => userID; set => userID = value; }

        /// <summary>
        /// The nickname of the player
        /// </summary>
        [JsonProperty("name")]
        public string DisplayName { get => nickname; set => nickname = value; }

        /// <summary>
        /// Character type ID in the library of characters (same as )
        /// </summary>
        /// <remarks>Same as the id in <see cref="PNPlayer.Character"/></remarks>
        [JsonProperty("char")]
        public int PlayerAvatarType { get => playerAvatarType; set => playerAvatarType = value; }

        /// <summary>
        /// Equipped Hat iD
        /// </summary>
        [JsonProperty("hat")]
        public int EquippedCosmetic { get => equippedCosmetic; set => equippedCosmetic = value; }

        /// <summary>
        /// Items that can be traded
        /// </summary>
        /// <remarks>as int IDs </remarks>       
        [JsonProperty("inventory")]
        public TradeInventoryData Inventory { get => tradeableItems; set => tradeableItems = value; }
    }
}