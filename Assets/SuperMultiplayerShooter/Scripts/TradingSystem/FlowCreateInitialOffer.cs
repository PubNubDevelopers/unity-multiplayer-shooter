using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PubNubUnityShowcase.UIComponents
{
    public class FlowCreateInitialOffer : FlowBase
    {
        private readonly string cmdSendOffer = "action-send";
        private readonly string cmdWitdraw = "action-witdraw";

        private OfferData _initialData;

        public FlowCreateInitialOffer(TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
        }

        public override void Load()
        {
            UI.OfferPanel.SetSessionStatus("Make your offer");

            //Set Flow
            SetOfferTransfersEnabled(true);
            UI.OfferPanel.AnyChange += OnAnyOfferChange;

            //Add buttons
            UI.Actions.AddButton(cmdCloseView, "Cancel", OnCloseRequest);
        }

        public override void Unload()
        {
            SetOfferTransfersEnabled(false);
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
                UI.Actions.AddButton(cmdSendOffer, "Propose", OnSendInitialOffer);
                SortButtons();
            }
            else
            {
                UI.Actions.RemoveButton(cmdSendOffer);
            }
        }

        private async void OnSendInitialOffer(string _)
        {
            UI.OfferPanel.SetSessionStatus("Sending offer...");
            UI.Actions.RemoveButton(cmdSendOffer);
            UI.Actions.RemoveButton(cmdCloseView);
            UI.Actions.AddButton(cmdWitdraw, "Withdraw", OnWitdraw);
            UI.Actions.SetButtonInteractable(cmdCloseView, false);

            UI.OfferPanel.SetLocked(true);
            UI.InitiatorInventory.SetVisibility(false);
            UI.RespondentInventory.SetVisibility(false);

            var cts = new CancellationTokenSource(10000);
            await Services.Trading.SendInviteAsync(OfferData.GenerateInitialOffer(UI.OfferPanel.InitiatorSlot.Item.ItemID, UI.OfferPanel.ResponderSlot.Item.ItemID, false));
            
            var _inviteResponseReceived = false;

            try
            {
                while (_inviteResponseReceived == false)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                ShowSessionResult($"No response from {SessionData.Respondent.DisplayName}");
                await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyNotResponding));
                UI.Actions.SetButtonInteractable(cmdCloseView, true);
            }
        }

        private void OnWitdraw(string _)
        {

        }

        private void SortButtons()
        {
            var priority = new List<string>
            {
                cmdCloseView,
                cmdSendOffer
            };
            UI.Actions.Arrange(priority);
        }
    }
}