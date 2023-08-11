using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PubNubUnityShowcase.UIComponents
{
    public class FlowOfferReceived : FlowBase
    {
        private readonly OfferData _receivedOffer;

        public FlowOfferReceived(OfferData offerReceived, TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
            _receivedOffer = offerReceived;
        }

        private OfferData ReceivedOffer => _receivedOffer;

        private bool SameOffer => _receivedOffer.InitiatorGives == UI.OfferPanel.InitiatorSlot.Item.ItemID && _receivedOffer.InitiatorReceives == UI.OfferPanel.ResponderSlot.Item.ItemID;

        public override void Load()
        {
            InitializeUIElements();
            BindOfferTransfers(true);

            RefreshAvatars(SessionData.Initiator, SessionData.Respondent);
            RefreshInventories(SessionData.Initiator.Inventory, SessionData.Respondent.Inventory);
            DisableDuplicateItems();

            UI.OfferPanel.SetSessionStatus($"{SessionData.Initiator.DisplayName}'s offer");
            FillOfferPanelFromInventories(ReceivedOffer);

            UI.OfferPanel.AnyChange += OnAnyOfferChange;

            //Add buttons
            UI.Actions.AddButton(cmdAccept, "Accept", OnBtnAccept);
            UI.Actions.AddButton(cmdRefuse, "Refuse", OnBtnRefuse);
            SortButtons();

            if (ReceivedOffer.Version > 0)
                UI.OfferPanel.SetLabel("Counteroffer");
            else
                UI.OfferPanel.SetLabel("Offer");

            bool canEdit = ReceivedOffer.Version > 2 || ReceivedOffer.FinalOffer;

            OfferSetLocked(canEdit);
            InventoriesVisibility(canEdit);
        }

        public override void Unload()
        {
            BindOfferTransfers(false);
            UI.Actions.RemoveAll();

            UI.OfferPanel.AnyChange -= OnAnyOfferChange;
        }

        private void OnAnyOfferChange()
        {
            if (SameOffer)
            {
                UI.Actions.RemoveButton(cmdCounter);
                UI.Actions.AddButton(cmdAccept, "Accept", OnBtnAccept);
                UI.Actions.SetButtonInteractable(cmdAccept, UI.OfferPanel.HaveValidOffer);
            }
            else
            {
                UI.Actions.RemoveButton(cmdAccept);
                UI.Actions.AddButton(cmdCounter, "Counter", OnBtnCounteroffer);
                UI.Actions.SetButtonInteractable(cmdCounter, UI.OfferPanel.HaveValidOffer); //this shoudn't be possible
            }

            SortButtons();
        }

        #region Button Actions
        private void OnBtnAccept(string _)
        {
            var offer = OfferData.GetAcceptedOffer(ReceivedOffer);
            Services.Trading.SendOfferAsync(offer);

            UI.Actions.RemoveAll();
        }

        private void OnBtnRefuse(string _)
        {
            var offer = OfferData.GetRejectedOffer(ReceivedOffer);
            Services.Trading.SendOfferAsync(offer);

            UI.Actions.RemoveAll();
        }

        private async void OnBtnCounteroffer(string _)
        {
            Debug.Log("----->>OnBtnCounter");

            var offer = OfferData.GenerateCounterlOffer(ReceivedOffer, UI.OfferPanel.InitiatorSlot.Item.ItemID, UI.OfferPanel.ResponderSlot.Item.ItemID, false);
            Debug.Log($"send: ---> offer");
            await Services.Trading.SendOfferAsync(offer);

            UI.Actions.RemoveAll();
            UI.Actions.AddButton(cmdWitdraw, "Withdraw", OnBtnWithdrawCounter);
            //UI.Actions.SetButtonInteractable(cmdWitdraw, false);

            OfferSetLocked(true);
            InventoriesVisibility(false);

            ReceivedOfferResponse = false;

            var cts = new CancellationTokenSource(30000); //30 sec hardcode timeout
            int time = 0;
            try
            {
                while (ReceivedOfferResponse == false)
                {
                    UI.OfferPanel.SetSessionStatus($"({time / 1000}) Awaiting response...");
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100);
                    time += 100;                    
                    await Task.Yield();
                }

                //UI.Actions.SetButtonInteractable(cmdWitdraw, true);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                ShowSessionResult($"No response from {offer.Target}");
                await Services.Trading.LeaveSessionAsync(new LeaveSessionData(SessionData.Initiator, LeaveReason.otherPartyNotResponding));
                UI.Actions.SetButtonInteractable(cmdWitdraw, true);
            }
        }

        private async void OnBtnWithdrawCounter(string _)
        {
            ReceivedOfferResponse = true;
            UI.Actions.RemoveButton(cmdWitdraw);
            ShowSessionResult($"offer withdrawn");

            var thisParticipant = SessionData.GetParticipant(ReceivedOffer.Target);
            LeaveSessionData leaveData = new LeaveSessionData(thisParticipant, LeaveReason.withdrawOffer);
            await Services.Trading.LeaveSessionAsync(leaveData);
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