using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PubNubUnityShowcase.UIComponents
{
    public class FlowCreateInitialOffer : FlowBase
    {
        public FlowCreateInitialOffer(TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
        }

        public override void Load()
        {
            UI.OfferPanel.SetSessionStatus("Make your offer");

            SetOfferTransfersEnabled(true);
            UI.OfferPanel.AnyChange += OnAnyOfferChange;

            //Add buttons
            UI.Actions.AddButton(cmdCancel, "Cancel", OnCloseRequest);
        }

        public override void Unload()
        {
            SetOfferTransfersEnabled(false);
            UI.Actions.RemoveAll();

            UI.OfferPanel.AnyChange -= OnAnyOfferChange;
        }

        private void OnCloseRequest(string _)
        {
            StateBase.InvokeCloseViewRequest();
        }

        private void OnAnyOfferChange()
        {
            if (UI.OfferPanel.HaveValidOffer)
            {
                UI.Actions.AddButton(cmdSendInitial, "Propose", OnSendInitialOffer);
                SortButtons();
            }
            else
            {
                UI.Actions.RemoveButton(cmdSendInitial);
            }
        }

        private async void OnSendInitialOffer(string _)
        {
            UI.OfferPanel.SetSessionStatus("Sending offer...");

            UI.Actions.RemoveButton(cmdSendInitial);
            UI.Actions.RemoveButton(cmdCancel);
            UI.Actions.AddButton(cmdWitdraw, "Withdraw", OnWitdraw);
            UI.Actions.SetButtonInteractable(cmdWitdraw, false);

            SetOfferLocked(true);

            var cts = new CancellationTokenSource(10000);
            await Services.Trading.SendInviteAsync(OfferData.GenerateInitialOffer(UI.OfferPanel.InitiatorSlot.Item.ItemID, UI.OfferPanel.ResponderSlot.Item.ItemID, false));
            
            TradeInviteResponceReceived = false;

            try
            {              
                while (TradeInviteResponceReceived == false)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                
                UI.Actions.SetButtonInteractable(cmdWitdraw, true);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                ShowSessionResult($"No response from {SessionData.Respondent.DisplayName}");
                await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyNotResponding));
                UI.Actions.SetButtonInteractable(cmdWitdraw, true);
            }
        }

        private async void OnWitdraw(string _)
        {
            UI.Actions.RemoveButton(cmdWitdraw);
            LeaveSessionData leaveData = new LeaveSessionData(SessionData.Initiator, LeaveReason.withdrawOffer);
            await Services.Trading.LeaveSessionAsync(leaveData);
            StateBase.InvokeCloseViewRequest();
        }

        private void SortButtons()
        {
            var priority = new List<string>
            {
                cmdCancel,
                cmdSendInitial,
            };
            UI.Actions.Arrange(priority);
        }
    }
}