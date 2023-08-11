using PubNubUnityShowcase.UIComponents;
using System;

namespace PubNubUnityShowcase
{
    public abstract class TradingViewStateBase
    {
        protected readonly string cmdCloseView = "action-close";
        protected readonly string cmdRefuse = "action-refuse";

        protected readonly OfferPanel offerPanel;
        protected readonly TraderInventoryPanel initiatorInventory;
        protected readonly TraderInventoryPanel respondentInventory;
        protected readonly ActionButtonsPanel actions;
        protected readonly TradingView.Services _services;
        private readonly TradingView.UIComponents _ui;

        protected TradingView.UIComponents UI => _ui;

        public event Action CloseViewRequested;

        protected TradingView.Services Services { get => _services; }
        protected TradingViewStateBase(TradingView.Services services, TradingView.UIComponents ui)
        {
            _ui = ui;
            this.offerPanel = ui.OfferPanel;
            this.initiatorInventory = ui.InitiatorInventory;
            this.respondentInventory = ui.RespondentInventory;
            this.actions = ui.Actions;

            _services = services;
        }

        public abstract void ApplyState();

        public void InvokeCloseViewRequest()
        {
            CloseViewRequested?.Invoke();
        }

        private void InvokeCloseViewRequest(string _)
        {
            CloseViewRequested?.Invoke();
        }

        protected void SetInvenotryToOfferFlow()
        {
            //Set Flow
            offerPanel.ItemTakenInitiator += OnOfferPanelInitiatorTaken;
            offerPanel.ItemTakenResponder += OnOfferPanelRespondentTaken;
            initiatorInventory.ItemTaken += OnInitiatorInventoryTake;
            respondentInventory.ItemTaken += OnRespondentInventoryTake;
        }

        public virtual void Dispose()
        {
            offerPanel.ItemTakenInitiator -= OnOfferPanelInitiatorTaken;
            offerPanel.ItemTakenResponder -= OnOfferPanelRespondentTaken;
            initiatorInventory.ItemTaken -= OnInitiatorInventoryTake;
            respondentInventory.ItemTaken -= OnRespondentInventoryTake;
        }


        protected void FillOfferPanelFromInventories(OfferData offerData)
        {
            //Fill offer panel
            CosmeticItem initiatorGives = Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorGives);
            initiatorInventory.RemoveItem(initiatorGives);
            offerPanel.InitiatorSlot.SetItem(initiatorGives);
            CosmeticItem initiatorReceives = Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorReceives);
            respondentInventory.RemoveItem(initiatorReceives);
            offerPanel.ResponderSlot.SetItem(initiatorReceives);
        }

        public void StateSessionComplete(string message)
        {
            actions.RemoveAll();
            actions.AddButton(cmdCloseView, "OK", InvokeCloseViewRequest);
            actions.SetButtonInteractable(cmdCloseView, true);
            initiatorInventory.SetVisibility(false);
            respondentInventory.SetVisibility(false);
            offerPanel.SetLocked(true);
            offerPanel.SetSessionStatus(message);
        }

        protected void OnOfferPanelInitiatorTaken(CosmeticItem item)
        {
            initiatorInventory.PutAnywhere(item);
            initiatorInventory.SetVisibility(true);
            respondentInventory.SetVisibility(true);
        }

        protected void OnOfferPanelRespondentTaken(CosmeticItem item)
        {
            respondentInventory.PutAnywhere(item);
            initiatorInventory.SetVisibility(true);
            respondentInventory.SetVisibility(true);
        }
        protected void OnInitiatorInventoryTake(CosmeticItem cosmeticItem)
        {
            if (offerPanel.InitiatorSlot.IsFull)
            {
                var temp = offerPanel.InitiatorSlot.Item;
                offerPanel.InitiatorSlot.SetEmpty();
                offerPanel.SetInitiatorGive(cosmeticItem);
                initiatorInventory.PutAnywhere(temp);
            }
            else
            {
                offerPanel.SetInitiatorGive(cosmeticItem);
            }
        }

        protected void OnRespondentInventoryTake(CosmeticItem cosmeticItem)
        {
            if (offerPanel.ResponderSlot.IsFull)
            {
                var temp = offerPanel.ResponderSlot.Item;
                offerPanel.ResponderSlot.SetEmpty();
                offerPanel.SetInitiatorReceive(cosmeticItem);
                respondentInventory.PutAnywhere(temp);
            }
            else
            {
                offerPanel.SetInitiatorReceive(cosmeticItem);
            }
        }
    }
}