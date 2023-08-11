using PubNubUnityShowcase.UIComponents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PubNubUnityShowcase
{
    public class TradingViewStateInitiator : TradingViewStateBase,
        ITradeSessionSubscriber,
        ITradeInviteSubscriber
    {
        private readonly string cmdSendOffer = "action-send";

        private readonly TradingViewData _viewData;

        private bool _inviteResponseReceived;

        private OfferData CounterOffer { get; set; }

        public TradingViewStateInitiator(TradingViewData viewData, TradingView.UIComponents ui) : base(viewData.Services, ui)
        {
            _viewData = viewData;
        }

        public override void ApplyState()
        {
            var session = Services.Trading.CreateSession(_viewData.Initiator, _viewData.Respondent);

            var initialOfferFlow = new FlowCreateInitialOffer(session, this, UI, Services);
            initialOfferFlow.Load();
           
            Services.Trading.SubscribeSessionEvents(this);
            Services.Trading.SubscribeTradeInvites(this);
            
            Services.Trading.JoinSessionAsync(session);
        }

        #region OnButtonAction Handlers
        //private async void OnBtnPropose(string _)
        //{
        //    offerPanel.SetSessionStatus("Sending offer...");
        //    actions.RemoveButton(cmdSendOffer);
        //    actions.ChangeButton(cmdCloseView, OnBtnClose, "Withdraw");
        //    actions.SetButtonInteractable(cmdCloseView, false);

        //    offerPanel.SetLocked(true);
        //    initiatorInventory.SetVisibility(false);
        //    respondentInventory.SetVisibility(false);

        //    var cts = new CancellationTokenSource(10000);
        //    await Services.Trading.SendInviteAsync(OfferData.GenerateInitialOffer(offerPanel.InitiatorSlot.Item.ItemID, offerPanel.ResponderSlot.Item.ItemID, false));
        //    _inviteResponseReceived = false;

        //    try
        //    {
        //        while (_inviteResponseReceived == false)
        //        {
        //            cts.Token.ThrowIfCancellationRequested();
        //            await Task.Yield();
        //        }
        //    }
        //    catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
        //    {
        //        StateSessionComplete($"No response from {_viewData.Respondent.DisplayName}");
        //        await Services.Trading.LeaveSessionAsync(new LeaveSessionData(_viewData.Initiator, LeaveReason.otherPartyNotResponding));
        //        actions.SetButtonInteractable(cmdCloseView, true);
        //    }
        //}

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

        private async void OnBtnWithdraw(string _)
        {
            actions.RemoveButton(cmdCloseView);

            LeaveSessionData leaveData = new LeaveSessionData(_viewData.Initiator, LeaveReason.withdrawOffer);
            await Services.Trading.LeaveSessionAsync(leaveData);
            InvokeCloseViewRequest();
        }

        //private void OnBtnClose(string _)
        //{
        //    InvokeCloseViewRequest();
        //}

        #endregion

        //private void SortButtons()
        //{
        //    var priority = new List<string>
        //    {
        //        cmdCloseView,
        //        cmdRefuse,
        //        cmdSendOffer
        //    };
        //    actions.Arrange(priority);
        //}

        public override void Dispose()
        {
            base.Dispose();
            Services.Trading.UnsubscribeTradeInvites(this);
            Services.Trading.UnsubscribeSessionEvents(this);
        }

        #region ITradeSessionSubscriber
        void ITradeSessionSubscriber.OnParticipantJoined(TraderData participant)
        {
            offerPanel.SetSessionStatus("Player checking your offer");
            actions.ChangeButton(cmdCloseView, OnBtnWithdraw, "Withdraw");
            actions.SetButtonInteractable(cmdCloseView, true);
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
            await Services.Trading.LeaveSessionAsync(new LeaveSessionData(_viewData.Initiator, LeaveReason.transactionComplete));
            
            if(offerData.State == OfferData.OfferState.accepted)
                StateSessionComplete($"Trade successfull");

            if (offerData.State == OfferData.OfferState.rejected)
                StateSessionComplete($"Trade Rejected");
        }

        void ITradeSessionSubscriber.OnCounterOffer(OfferData offerData)
        {
            CounterOffer = offerData;

            offerPanel.SetSessionStatus($"{_viewData.Respondent.DisplayName}'s counter offer");
            offerPanel.SetLabel("counter offer");
            offerPanel.SetLocked(false);

            FillOfferPanelFromInventories(offerData);

            //hide inventories initially
            initiatorInventory.SetVisibility(false);
            respondentInventory.SetVisibility(false);

            //Reset buttons
            actions.AddButton(cmdSendOffer, "Accept", OnBtnAcceptCounteroffer);
            actions.AddButton(cmdRefuse, "Refuse", OnBtnRefuseCounteroffer);
            actions.RemoveButton(cmdCloseView);
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
            _inviteResponseReceived = true;

            if (response.WillJoin == false)
            {
                //why?
                if (response.InGameOrLobby)
                {
                    StateSessionComplete($"{_viewData.Respondent.DisplayName} is in Match");
                    await Services.Trading.LeaveSessionAsync(new LeaveSessionData(_viewData.Initiator, LeaveReason.otherPartyBusy));
                }

                else if (response.InTradingSession)
                {
                    StateSessionComplete($"{_viewData.Respondent.DisplayName} is Trading with another player");
                    await Services.Trading.LeaveSessionAsync(new LeaveSessionData(_viewData.Initiator, LeaveReason.otherPartyBusy));
                }
            }
        }
        #endregion
    }
}
