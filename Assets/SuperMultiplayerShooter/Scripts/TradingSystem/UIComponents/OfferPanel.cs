using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

namespace PubNubUnityShowcase.UIComponents
{
    public class OfferPanel : MonoBehaviour
    {
        [SerializeField] private InventorySlot initiatorSlot;
        [SerializeField] private InventorySlot responderSlot;
        [SerializeField] private Text labelText;
        [SerializeField] private Text sessionStatusText; //TODO: move this to different UIElement (Trading Status panel)

        public InventorySlot InitiatorSlot => initiatorSlot;
        public InventorySlot ResponderSlot => responderSlot;

        public bool HaveValidOffer
        {
            get
            {
                bool filled = (initiatorSlot.IsFull) && (responderSlot.IsFull);
                bool diff = !initiatorSlot.Item.Equals(responderSlot.Item);

                return filled && diff;
            }
        }              

        public event Action<CosmeticItem> ItemTakenInitiator;
        public event Action<CosmeticItem> ItemTakenResponder;

        public event Action AnyChange;

        public void Construct()
        {
            initiatorSlot.Construct(0, $"--slot-initiator");
            initiatorSlot.SetEmpty();
            initiatorSlot.SetClickInteraction(OnInitiatorClicked);
            initiatorSlot.ContentChanged += OnAnyChange;

            responderSlot.Construct(0, $"--slot-responder");
            responderSlot.SetEmpty();
            responderSlot.SetClickInteraction(OnRespondentClicked);
            responderSlot.ContentChanged += OnAnyChange;
        }

        public void EmptySlots(bool initiator, bool responder)
        {
            if (initiator)
                InitiatorSlot.SetEmpty();

            if (responder)
                ResponderSlot.SetEmpty();
        }

        public void LoadOffer(CosmeticItem initiator, CosmeticItem responder)
        {
            InitiatorSlot.SetItem(initiator);
            InitiatorSlot.SetItem(responder);
        }

        public bool SetInitiatorGive(CosmeticItem item)
        {
            if (!InitiatorSlot.IsFull)
            {
                InitiatorSlot.SetItem(item);
                return true;
            }
            else
                return false;
        }

        public bool SetInitiatorReceive(CosmeticItem item)
        {
            if (!ResponderSlot.IsFull)
            {
                ResponderSlot.SetItem(item);
                return true;
            }
            else
                return false;
        }

        private void OnInitiatorClicked(int _)
        {
            if(InitiatorSlot.IsFull)
            {
                ItemTakenInitiator?.Invoke(InitiatorSlot.Item);
                InitiatorSlot.SetEmpty();
            }
        }

        private void OnRespondentClicked(int _)
        {
            if (ResponderSlot.IsFull)
            {
                ItemTakenResponder?.Invoke(ResponderSlot.Item);
                ResponderSlot.SetEmpty();
            }
        }

        public void SetLocked(bool state)
        {
            initiatorSlot.SetLocked(state);
            responderSlot.SetLocked(state);
        }

        public void SetLabel(string str)
        {
            labelText.text = str;
        }

        public void SetSessionStatus(string str)
        {
            sessionStatusText.text = str;
        }

        private void OnAnyChange()
        {
            AnyChange?.Invoke();
        }
    }
}
