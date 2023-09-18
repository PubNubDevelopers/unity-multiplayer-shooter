using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase.ScriptableObjects
{
    /// <summary>
    /// Library for cosmetic items
    /// </summary>
    [CreateAssetMenu(fileName = "lib-cometicItems-", menuName = "Assets/CosmeticItems Library")]
    public class CosmeticsLibrary : ScriptableObject,
        ICosmeticItemLibrary,
        IAvatarLibrary
    {
        [SerializeField] private List<CosmeticItem> items;
        [SerializeField] private List<AvatarGraphics> avatars;

        public CosmeticItem GetCosmeticItem(int id)
        {
            return items.Find(s => s.ItemID == id);
        }

        public AvatarGraphics GetAvatar(int id)
        {
            return avatars.Find(s => s.ItemID == id);
        }
    }
}
