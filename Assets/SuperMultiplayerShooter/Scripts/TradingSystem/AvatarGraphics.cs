using UnityEngine;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct AvatarGraphics
    {
        [SerializeField] private int itemID;
        [SerializeField] private Sprite uiSprite; //Sprite for UI

        public int ItemID { get => itemID; }
        public Sprite UiSprite { get => uiSprite; }
    }
}