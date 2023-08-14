using System;
using UnityEngine;
using UnityEngine.UI;

namespace PubNubUnityShowcase.UIComponents
{

    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private Image contentImage;
        [SerializeField] private Button button;

        private Action<int> clickCallback;

        public bool IsFull { get; private set; }
        public CosmeticItem Item { get; private set; }
        private bool AllowClicks { get; set; }

        private int _indexInGrid;

        private static Color ColNormal = Color.white;
        private static Color ColGrey = new Color(0.4f, 0.4f, 0.4f, 1f);

        public event Action ContentChanged;

        public void Construct(int indexInGrid, string goName = "--slot")
        {
            _indexInGrid = indexInGrid;

            gameObject.name = goName;

            if (button != null)
                button.onClick.AddListener(OnBtnClick);

            AllowClicks = true;
        }

        public void SetItemGreyed(bool state)
        {
            if (IsFull)
                contentImage.color = state ? ColGrey : ColNormal;
            else
                SetEmpty();
        }

        public void SetLocked(bool state)
        {
            AllowClicks = !state;
        }

        public void SetEmpty()
        {
            IsFull = false;

            contentImage.color = new Color(1, 1, 1, 0);
            ContentChanged?.Invoke();
        }

        public void SetItem(CosmeticItem item)
        {
            Item = item;
            IsFull = true;

            contentImage.color = new Color(1, 1, 1, 1);
            contentImage.sprite = item.UiSprite;
            ContentChanged?.Invoke();
        }

        public void SetClickInteraction(Action<int> callback)
        {
            clickCallback += callback;
        }

        public void RemoveClickInteractions()
        {
            clickCallback = null;
        }

        private void OnBtnClick()
        {
            //if the slot is empty it won't send events
            if (AllowClicks && IsFull)
                clickCallback?.Invoke(_indexInGrid);
        }
    }
}
