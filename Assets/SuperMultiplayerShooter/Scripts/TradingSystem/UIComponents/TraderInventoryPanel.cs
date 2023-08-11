using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PubNubUnityShowcase.UIComponents
{
    public class TraderInventoryPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private InventorySlot slotPrefab; //TODO: inject
        [SerializeField] private CanvasGroup panelGroup;

        private int _slotCount = 8;
        private ICosmeticItemLibrary _cosmeticsLibrary; //TODO: inject
        private Dictionary<int, InventorySlot> slots = new Dictionary<int, InventorySlot>();

        public event Action<CosmeticItem> ItemTaken;

        public void Construct(ICosmeticItemLibrary cosmeticsLibrary, int maxSlots = 8)
        {
            _cosmeticsLibrary = cosmeticsLibrary;
            _slotCount = maxSlots;

            for (int i = 0; i <= _slotCount - 1; i++)
            {
                var slot = Instantiate(slotPrefab);
                slot.transform.SetParent(gridRoot);
                slot.Construct(i, $"--slot-[{i}]");
                slot.SetClickInteraction(OnClick);
                slots.Add(i, slot);
            }
        }

        public void SetVisibility(bool state)
        {
            panelGroup.alpha = state ? 1 : 0;
        }

        public void SetInteraction(bool state)
        {
            foreach (var slot in slots.Values)
            {
                slot.SetItemGreyed(!state);
            }

            panelGroup.interactable = state;
        }

        public void SetInteraction(int slot, bool state)
        {
            slots[slot].SetItemGreyed(state);
        }

        private void OnClick(int slotIndex)
        {
            //Debug.Log($"Clicked index: {slotIndex}");
            var item = slots[slotIndex].Item;
            slots[slotIndex].SetEmpty();
            ItemTaken?.Invoke(item);
        }

        public void PutAnywhere(CosmeticItem item)
        {
            var emptySlot = slots.First(s => !s.Value.IsFull).Value;
            emptySlot.SetItem(item);
        }

        public void RemoveItem(CosmeticItem item)
        {
            var itemSlot = slots.First(s => s.Value.Item.Equals(item)).Value;
            itemSlot.SetEmpty();
        }

        public List<int> CheckDuplicates(List<CosmeticItem> items, bool andDisableSlot = false)
        {
            List<int> duplicates = new List<int>();

            foreach (var item in items)
            {
                foreach (var slot in slots.Values)
                {
                    if (!slot.IsFull)
                        continue;

                    if (item.Equals(slot.Item))
                    {
                        slot.SetItemGreyed(andDisableSlot);
                        slot.RemoveClickInteractions();
                        duplicates.Add(item.ItemID);
                    }
                }
            }
            return duplicates;
        }

        public List<CosmeticItem> GetCosmetics()
        {
            List<CosmeticItem> results = new List<CosmeticItem>();

            foreach (var slot in slots.Values)
            {
                if (!slot.IsFull)
                    continue;

                results.Add(slot.Item);
            }

            return results;
        }

        public void UpdateData(TradeInventoryData data)
        {
            if (data.CosmeticItems.Count <= 0 || _cosmeticsLibrary == null)
            {
                Debug.LogWarning("Data error");
                return;
            }

            for (int i = 0; i <= slots.Count - 1; i++)
            {
                bool isEmpty = i >= data.CosmeticItems.Count;

                if (!isEmpty)
                {
                    CosmeticItem itemData = _cosmeticsLibrary.GetCosmeticItem(data.CosmeticItems[i]);
                    slots[i].SetItem(itemData);
                }
                else
                    slots[i].SetEmpty();
            }
        }
    }
}
