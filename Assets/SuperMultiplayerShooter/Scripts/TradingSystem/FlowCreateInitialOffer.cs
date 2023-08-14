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
            InitializeUIElements();
            BindOfferTransfers(true);

            RefreshAvatars(SessionData.Initiator, SessionData.Respondent);
            RefreshInventories(SessionData.Initiator.Inventory, SessionData.Respondent.Inventory);
            DisableDuplicateItems();

            UI.OfferPanel.SetSessionStatus("Make your offer");

            
            UI.OfferPanel.AnyChange += OnAnyOfferChange;

            //Add buttons
            UI.Actions.AddButton(cmdCancel, "Cancel", OnBtnClose);
        }

        public override void Unload()
        {
            BindOfferTransfers(false);
            UI.Actions.RemoveAll();

            UI.OfferPanel.AnyChange -= OnAnyOfferChange;
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
            UI.Actions.RemoveButton(cmdSendInitial);
            UI.Actions.RemoveButton(cmdCancel);
            UI.Actions.AddButton(cmdWitdraw, "Withdraw", OnWitdraw);
            UI.Actions.SetButtonInteractable(cmdWitdraw, false);

            OfferSetLocked(true);
            InventoriesVisibility(false);

            var cts = new CancellationTokenSource(10000);

            var sentOffer = OfferData.GenerateInitialOffer(UI.OfferPanel.InitiatorSlot.Item.ItemID, UI.OfferPanel.ResponderSlot.Item.ItemID, false);
            await Services.Trading.SendInviteAsync(sentOffer);
            
            TradeInviteResponceReceived = false;
            int time = 0;

            try
            {              
                while (TradeInviteResponceReceived == false)
                {
                    string targetName = SessionData.GetParticipant(sentOffer.Target).DisplayName;

                    UI.OfferPanel.SetSessionStatus($"Awaiting {targetName}'s response ({time / 1000})");
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100);
                    time += 100;                    
                    await Task.Yield();
                }
                
                UI.Actions.SetButtonInteractable(cmdWitdraw, true);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                ShowSessionResult($"No response from {SessionData.Respondent.DisplayName}");
                await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyNotResponding));                
            }
        }

        private async void OnWitdraw(string _)
        {
            UI.Actions.RemoveButton(cmdWitdraw);
            ShowSessionResult($"offer withdrawn");

            LeaveSessionData leaveData = new LeaveSessionData(SessionData.Initiator, LeaveReason.withdrawOffer);
            await Services.Trading.LeaveSessionAsync(leaveData);            
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