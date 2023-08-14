using System;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct CosmeticItem
    {
        [SerializeField] private int itemID;
        //[SerializeField] private string name;
        [SerializeField] private CosmeticType type;
        [SerializeField] private Sprite uiSprite; //Sprite for UI
        [SerializeField] private float offsetY; //workaround so that hats can align with the body
        [SerializeField] private GameObject prefab; //ingame object

        public int ItemID { get => itemID; }
        public CosmeticType Type { get => type; }
        public Sprite UiSprite { get => uiSprite; }
        public float OffsetY { get => offsetY; }

        public override bool Equals(object obj)
        {
            return obj is CosmeticItem item &&
                   itemID == item.itemID &&
                   type == item.type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(itemID, type);
        }
    }
}
