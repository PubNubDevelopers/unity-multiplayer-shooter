using System.Collections.Generic;

namespace PubNubUnityShowcase.UIComponents
{
    public class FlowInitialOfferReceived : FlowBase
    {
        private readonly OfferData _receivedOffer;

        private OfferData ReceivedOffer => _receivedOffer;

        private bool SameOffer => _receivedOffer.InitiatorGives == UI.OfferPanel.InitiatorSlot.Item.ItemID && _receivedOffer.InitiatorReceives == UI.OfferPanel.ResponderSlot.Item.ItemID;
        public FlowInitialOfferReceived(OfferData offerReceived, TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
            _receivedOffer = offerReceived;
        }

        public override void Load()
        {
            UI.OfferPanel.SetSessionStatus($"{SessionData.Initiator.DisplayName}'s offer");
            FillOfferPanelFromInventories(ReceivedOffer);

            UI.OfferPanel.AnyChange += OnAnyOfferChange;

            //hide inventories
            UI.InitiatorInventory.SetVisibility(false);
            UI.RespondentInventory.SetVisibility(false);

            //Add buttons
            UI.Actions.AddButton(cmdAccept, "Accept", OnBtnAccept);
            UI.Actions.AddButton(cmdRefuse, "Refuse", OnBtnRefuse);
            SortButtons();

            SetOfferTransfersEnabled(!ReceivedOffer.FinalOffer);
        }

        public override void Unload()
        {
            SetOfferTransfersEnabled(false);
            UI.Actions.RemoveAll();

            UI.OfferPanel.AnyChange -= OnAnyOfferChange;
        }

        private void OnAnyOfferChange()
        {
            UI.Actions.SetButtonInteractable(cmdAccept, UI.OfferPanel.HaveValidOffer);

            if (UI.OfferPanel.HaveValidOffer)
            {
                if (SameOffer)
                {
                    UI.Actions.RemoveButton(cmdCounter);
                    UI.Actions.AddButton(cmdAccept, "Accept", OnBtnAccept);
                }
                else
                {
                    UI.Actions.RemoveButton(cmdAccept);
                    UI.Actions.AddButton(cmdCounter, "Counter", OnBtnCounteroffer);
                }

                SortButtons();
            }
        }

        #region Button Actions
        private void OnBtnAccept(string _)
        {
            var offer = OfferData.GetAcceptedOffer(ReceivedOffer);
            Services.Trading.SendOfferAsync(offer);
        }

        private void OnBtnRefuse(string _)
        {
            var offer = OfferData.GetRejectedOffer(ReceivedOffer);
            Services.Trading.SendOfferAsync(offer);
        }
        private void OnBtnCounteroffer(string _)
        {
            var offer = OfferData.GenerateCounterlOffer(ReceivedOffer, UI.OfferPanel.InitiatorSlot.Item.ItemID, UI.OfferPanel.ResponderSlot.Item.ItemID, false);
            Services.Trading.SendOfferAsync(offer);
        }
        #endregion

        private void SortButtons()
        {
            var priority = new List<string>
            {
                cmdRefuse,
                cmdCounter,
                cmdAccept
            };

            UI.Actions.Arrange(priority);
        }
    }
}