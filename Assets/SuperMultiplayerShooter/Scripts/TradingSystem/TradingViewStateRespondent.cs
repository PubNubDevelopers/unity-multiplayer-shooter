using PubNubUnityShowcase.UIComponents;
using UnityEngine;

namespace PubNubUnityShowcase
{
    public class TradingViewStateRespondent : TradingViewStateBase,
        ITradeSessionSubscriber
    {
        private readonly string cmdAccept = "action-accept";
        //private readonly string cmdCounter = "action-counter";
        private OfferData _currentOffer;

        private readonly TradingViewData _viewData;


        private bool SameOffer => _currentOffer.InitiatorGives == offerPanel.InitiatorSlot.Item.ItemID && _currentOffer.InitiatorReceives == offerPanel.ResponderSlot.Item.ItemID;

        public TradingViewStateRespondent(OfferData originalOffer, TradingViewData viewData, TradingView.UIComponents ui) : base(viewData.Services, ui)
        {
            _currentOffer = originalOffer;
            _viewData = viewData;
        }

        public override void ApplyState()
        {
            //Initial Title
            offerPanel.SetSessionStatus($"{_viewData.Initiator.DisplayName}'s offer");
            SetInvenotryToOfferFlow();



            FillOfferPanelFromInventories(_currentOffer);

            offerPanel.AnyChange += OnAnyOfferChange;

            //hide inventories initially
            initiatorInventory.SetVisibility(false);
            respondentInventory.SetVisibility(false);

            actions.AddButton(cmdAccept, "Accept", OnBtnAccept);
            actions.AddButton(cmdRefuse, "Refuse", OnBtnRefuse);

            Services.Trading.SubscribeSessionEvents(this);
        }

        private void OnAnyOfferChange()
        {
            actions.SetButtonInteractable(cmdAccept, offerPanel.HaveValidOffer);

            if (offerPanel.HaveValidOffer)
            {
                bool sameOffer = _currentOffer.InitiatorGives == offerPanel.InitiatorSlot.Item.ItemID && _currentOffer.InitiatorReceives == offerPanel.InitiatorSlot.Item.ItemID;

                if (SameOffer)
                {
                    actions.ChangeButton(cmdAccept, OnBtnAccept, "Accept");
                }
                else
                {
                    actions.ChangeButton(cmdAccept, OnBtnCounteroffer, "Counter");
                }
                //SortButtons();
            }
        }

        void ITradeSessionSubscriber.OnParticipantJoined(TraderData participant)
        {
            Debug.LogWarning("Respondent should always join second. Something went wrong");
        }

        async void ITradeSessionSubscriber.OnParticipantGoodbye(LeaveSessionData leaveData)
        {
            Debug.Log($"{leaveData.Participant.DisplayName} Left the trade: {leaveData.Reason}");

            if (leaveData.Reason == LeaveReason.withdrawOffer)
            {
                LeaveSessionData myLeave = new LeaveSessionData(_viewData.Initiator, LeaveReason.withdrawOffer);
                await Services.Trading.LeaveSessionAsync(myLeave); //You leave too
                StateSessionComplete($"Offer Withdraw");
            }
        }

        void ITradeSessionSubscriber.OnLeftUnknownReason(PubNubUnityShowcase.TraderData participant)
        {
            Debug.Log("LeftUnknown");
        }

        async void ITradeSessionSubscriber.OnTradingCompleted(OfferData offerData)
        {
            await Services.Trading.LeaveSessionAsync(new LeaveSessionData(_viewData.Initiator, LeaveReason.transactionComplete));
            if (offerData.State == OfferData.OfferState.accepted)
                StateSessionComplete($"Trade successfull");

            if (offerData.State == OfferData.OfferState.rejected)
                StateSessionComplete($"Trade Rejected");
        }

        void ITradeSessionSubscriber.OnCounterOffer(OfferData offerData)
        {
            offerPanel.SetLabel("counter offer");
        }

        private void OnBtnAccept(string _)
        {
            var offer = OfferData.GetAcceptedOffer(_currentOffer);
            Services.Trading.SendOfferAsync(offer);
        }

        private void OnBtnCounteroffer(string _)
        {
            var offer = OfferData.GenerateCounterlOffer(_currentOffer, offerPanel.InitiatorSlot.Item.ItemID, offerPanel.ResponderSlot.Item.ItemID, false);
            Services.Trading.SendOfferAsync(offer);
        }

        private void OnBtnRefuse(string _)
        {
            var offer = OfferData.GetRejectedOffer(_currentOffer);
            Services.Trading.SendOfferAsync(offer);
        }

        private void OnBtnClose(string _)
        {
            InvokeCloseViewRequest();
        }

        public override void Dispose()
        {
            base.Dispose();
            Services.Trading.UnsubscribeSessionEvents(this);
        }
    }
}