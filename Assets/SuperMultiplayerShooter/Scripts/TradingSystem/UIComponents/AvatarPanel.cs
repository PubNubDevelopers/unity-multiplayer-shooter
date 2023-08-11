using PubNubUnityShowcase.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace PubNubUnityShowcase.UIComponents
{
    public class AvatarPanel : MonoBehaviour
    {
        //[SerializeField] private RectTransform hatRoot;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hatImage;
        [SerializeField] private Text nicknameText;

        private IAvatarLibrary _avatars;
        private ICosmeticItemLibrary _cosmetics;

        private Vector3 initialPos;

        private InventorySlot _linkedSlot;
        private int _fallbackCosmetic;
        private TraderInventoryPanel _ownedInventory;

        private void Awake()
        {
            initialPos = hatImage.transform.localPosition;
        }

        public void Construct(IAvatarLibrary avatars, ICosmeticItemLibrary cosmetics, InventorySlot slot, TraderInventoryPanel ownedInventory, int fallbackCosmetic)
        {
            _avatars = avatars;
            _cosmetics = cosmetics;
            _linkedSlot = slot;
            _ownedInventory = ownedInventory;
            _fallbackCosmetic = fallbackCosmetic;

            _linkedSlot.ContentChanged += OnSlotContentChange;
            _ownedInventory.ItemTaken += OnTakenFromInventory;
        }

        public void SetBody(int body)
        {
            var bodyData = _avatars.GetAvatar(body);

            bodyImage.sprite = bodyData.UiSprite;

            //note: also set hat root if needed
        }

        /// <summary>
        /// Sets look direction to left or right
        /// </summary>
        /// <param name="dir"></param>
        /// <remarks>set either 1 or -1</remarks>
        public void SetLookDirection(int dir = 1)
        {
            bodyImage.transform.localScale = new Vector3(dir, 1, 1);
            hatImage.transform.localScale = new Vector3(dir, 1, 1);
        }

        public void SetHat(int hat)
        {
            CosmeticItem hatData = _cosmetics.GetCosmeticItem(hat);

            hatImage.sprite = hatData.UiSprite;
            hatImage.transform.localPosition = new Vector3(initialPos.x, initialPos.y + hatData.OffsetY, initialPos.z);
        }

        public void SetHatVisibility(bool state)
        {
            hatImage.color = state ? new Color(1, 1, 1, 0) : Color.white;
        }

        private void OnSlotContentChange()
        {
            //SetHatVisibility(_linkedSlot.Item.ItemID == _fallbackCosmetic || !_linkedSlot.IsFull);

            if (_linkedSlot.IsFull)
                SetHat(_linkedSlot.Item.ItemID);
            else
                SetHat(_fallbackCosmetic);
        }

        public void SetNickname(string nickname)
        {
            nicknameText.text = nickname;
        }

        private void OnTakenFromInventory(CosmeticItem item)
        {
            //SetHatVisibility(item.ItemID == _fallbackCosmetic);
        }


        private void OnDisable()
        {
            if (_linkedSlot != null)
                _linkedSlot.ContentChanged -= OnSlotContentChange;
        }
    }
}