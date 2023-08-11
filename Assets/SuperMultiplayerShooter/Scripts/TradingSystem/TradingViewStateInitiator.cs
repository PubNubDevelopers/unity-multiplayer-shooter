using PubNubUnityShowcase.UIComponents;
using UnityEngine;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Initiator Session Controller
    /// </summary>
    public class TradingViewStateInitiator : TradingViewStateBase,
        ITradeSessionSubscriber,
        ITradeInviteSubscriber
    {
        private OfferData CounterOffer { get; set; }

        public TradingViewStateInitiator(TradeSessionData sessionData, TradingView.Services services, TradingView.UIComponents ui) : base(sessionData, services, ui)
        {
            Flow = new FlowCreateInitialOffer(sessionData, this, ui, services);
            Flow.Load();

            Services.Trading.SubscribeSessionEvents(this);
            Services.Trading.SubscribeTradeInvites(this);            
        }

        #region OnButtonAction Handlers

        private void OnBtnAcceptCounteroffer(string _)
        {
            var offer = OfferData.GetAcceptedOffer(CounterOffer);
            Services.Trading.SendOfferAsync(offer);
        }

        private void OnBtnRefuseCounteroffer(string _)
        {
            var offer = OfferData.GetRejectedOffer(CounterOffer);
            Services.Trading.SendOfferAsync(offer);
        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();
            Services.Trading.UnsubscribeTradeInvites(this);
            Services.Trading.UnsubscribeSessionEvents(this);
        }

        #region ITradeSessionSubscriber
        void ITradeSessionSubscriber.OnParticipantJoined(TraderData participant)
        {

        }

        void ITradeSessionSubscriber.OnParticipantGoodbye(LeaveSessionData leaveData)
        {
            Debug.Log($"{leaveData.Participant.DisplayName} Left the trade: {leaveData.Reason}");
        }

        void ITradeSessionSubscriber.OnLeftUnknownReason(TraderData participant)
        {

        }

        async void ITradeSessionSubscriber.OnTradingCompleted(OfferData offerData)
        {
            await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.transactionComplete));

            if (offerData.State == OfferData.OfferState.accepted)
                StateSessionComplete($"Trade successfull");

            if (offerData.State == OfferData.OfferState.rejected)
                StateSessionComplete($"Trade Rejected");
        }

        void ITradeSessionSubscriber.OnCounterOffer(OfferData offerData)
        {
            Debug.LogWarning("Received Counteroffer");

            Flow.Unload();
            Flow = new FlowInitialOfferReceived(offerData, SessionData, this, UI, Services);
            Flow.Load();

            //{
            //    CounterOffer = offerData;

            //    offerPanel.SetSessionStatus($"{SessionData.Respondent.DisplayName}'s counter offer");
            //    offerPanel.SetLabel("counter offer");
            //    offerPanel.SetLocked(false);

            //    FillOfferPanelFromInventories(offerData);

            //    //hide inventories initially
            //    initiatorInventory.SetVisibility(false);
            //    respondentInventory.SetVisibility(false);

            //    //Reset buttons
            //    actions.AddButton(FlowBase.cmdAccept, "Accept", OnBtnAcceptCounteroffer);
            //    actions.AddButton(FlowBase.cmdRefuse, "Refuse", OnBtnRefuseCounteroffer);
            //    actions.RemoveButton(FlowBase.cmdCancel);
            //}
        }

        #endregion

        #region ITradeInviteSubscriber
        void ITradeInviteSubscriber.OnTradeInviteReceived(TradeInvite invite)
        {
            //Send message that the user is busy trading (since this view can't be opened inGameOrLobby is asumed false)
            Services.Trading.InviteRespondAsync(invite, new InviteResponseData(false, true, false));
        }

        void ITradeInviteSubscriber.OnTradeInviteWithdrawn(TradeInvite invite)
        {
            //self close will be handled elsewhere
        }

        async void ITradeInviteSubscriber.OntradeInviteResponse(InviteResponseData response)
        {
            //only handle refuse to join (joins will handled as ITradeSessionSubscriber)
            Debug.Log($"Respondent: json={((IJsonSerializable)response).RawJson}");
            Flow.TradeInviteResponceReceived = true;

            if (response.WillJoin == false)
            {
                //why?
                if (response.InGameOrLobby)
                {
                    StateSessionComplete($"{SessionData.Respondent.DisplayName} is in Match");
                    await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyBusy));
                }

                else if (response.InTradingSession)
                {
                    StateSessionComplete($"{SessionData.Respondent.DisplayName} is Trading with another player");
                    await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyBusy));
                }
            }
            else
            {
                offerPanel.SetSessionStatus($"{SessionData.Respondent.DisplayName} checking your offer");

            }
        }
        #endregion
    }
}
