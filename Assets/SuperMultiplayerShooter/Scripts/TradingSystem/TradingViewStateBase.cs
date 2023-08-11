using PubNubUnityShowcase.UIComponents;
using System;

namespace PubNubUnityShowcase
{
    public abstract class TradingViewStateBase
    {

        private readonly TradeSessionData _sessionData;
        private readonly TradingView.UIComponents _ui;
        private FlowBase _flow;
        protected readonly OfferPanel offerPanel;
        protected readonly TraderInventoryPanel initiatorInventory;
        protected readonly TraderInventoryPanel respondentInventory;
        protected readonly ActionButtonsPanel actions;
        protected readonly TradingView.Services _services;

        protected FlowBase Flow { get => _flow; set => _flow = value; }
        protected TradeSessionData SessionData => _sessionData;

        protected TradingView.UIComponents UI => _ui;

        public event Action CloseViewRequested;

        protected TradingView.Services Services { get => _services; }
        protected TradingViewStateBase(TradeSessionData sessionData, TradingView.Services services, TradingView.UIComponents ui)
        {
            _sessionData = sessionData;
            _ui = ui;
            this.offerPanel = ui.OfferPanel;
            this.initiatorInventory = ui.InventoryInitiator;
            this.respondentInventory = ui.InventoryRespondent;
            this.actions = ui.Actions;

            _services = services;
        }

        public void Join()
        {
            Services.Trading.JoinSessionAsync(SessionData);
        }

        public void InvokeCloseViewRequest()
        {
            CloseViewRequested?.Invoke();
        }

        private void InvokeCloseViewRequest(string _)
        {
            CloseViewRequested?.Invoke();
        }

        public virtual void Dispose()
        {
            Flow.Unload();
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
            actions.AddButton(FlowBase.cmdOK, "OK", InvokeCloseViewRequest);
            actions.SetButtonInteractable(FlowBase.cmdCancel, true);
            initiatorInventory.SetVisibility(false);
            respondentInventory.SetVisibility(false);
            offerPanel.SetLocked(true);
            offerPanel.SetSessionStatus(message);
        }

        //protected void OnOfferPanelInitiatorTaken(CosmeticItem item)
        //{
        //    initiatorInventory.PutAnywhere(item);
        //    initiatorInventory.SetVisibility(true);
        //    respondentInventory.SetVisibility(true);
        //}

        //protected void OnOfferPanelRespondentTaken(CosmeticItem item)
        //{
        //    respondentInventory.PutAnywhere(item);
        //    initiatorInventory.SetVisibility(true);
        //    respondentInventory.SetVisibility(true);
        //}
        //protected void OnInitiatorInventoryTake(CosmeticItem cosmeticItem)
        //{
        //    if (offerPanel.InitiatorSlot.IsFull)
        //    {
        //        var temp = offerPanel.InitiatorSlot.Item;
        //        offerPanel.InitiatorSlot.SetEmpty();
        //        offerPanel.SetInitiatorGive(cosmeticItem);
        //        initiatorInventory.PutAnywhere(temp);
        //    }
        //    else
        //    {
        //        offerPanel.SetInitiatorGive(cosmeticItem);
        //    }
        //}

        //protected void OnRespondentInventoryTake(CosmeticItem cosmeticItem)
        //{
        //    if (offerPanel.ResponderSlot.IsFull)
        //    {
        //        var temp = offerPanel.ResponderSlot.Item;
        //        offerPanel.ResponderSlot.SetEmpty();
        //        offerPanel.SetInitiatorReceive(cosmeticItem);
        //        respondentInventory.PutAnywhere(temp);
        //    }
        //    else
        //    {
        //        offerPanel.SetInitiatorReceive(cosmeticItem);
        //    }
        //}
    }
}