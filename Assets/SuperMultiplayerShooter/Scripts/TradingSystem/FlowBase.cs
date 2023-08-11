using PubNubUnityShowcase;
using PubNubUnityShowcase.UIComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PubNubUnityShowcase.UIComponents.TradingView;

public abstract class FlowBase
{
    public static string cmdCancel = "action-close";
    public static string cmdOK = "action-OK";
    public static string cmdSendInitial = "action-sendInitial";
    public static string cmdWitdraw = "action-witdraw";
    public static string cmdAccept = "action-accept";
    public static string cmdCounter = "action-counter";
    public static string cmdRefuse = "action-refuse";
    

    private readonly UIComponents _ui;
    private readonly Services _services;
    private readonly TradingViewStateBase _stateBase;
    private readonly TradeSessionData _sessionData;
    protected TradingViewStateBase StateBase => _stateBase;
    protected TradeSessionData SessionData => _sessionData;
    protected UIComponents UI => _ui;
    protected Services Services => _services;
    public bool TradeInviteResponceReceived { get; set; }
    public bool ReceivedCounterofferResponse { get; set; }

    protected FlowBase(TradeSessionData sessionData, TradingViewStateBase stateBase, UIComponents ui, Services services)
    {
        _sessionData = sessionData;
        _ui = ui;
        _services = services;
        _stateBase = stateBase;
        _sessionData = sessionData;
    }

    public abstract void Load();
    public abstract void Unload();


    protected void FillOfferPanelFromInventories(OfferData offerData)
    {
        //Fill offer panel
        CosmeticItem initiatorGives = Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorGives);
        UI.InitiatorInventory.RemoveItem(initiatorGives);
        UI.OfferPanel.InitiatorSlot.SetItem(initiatorGives);

        CosmeticItem initiatorReceives = Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorReceives);
        UI.RespondentInventory.RemoveItem(initiatorReceives);
        UI.OfferPanel.ResponderSlot.SetItem(initiatorReceives);
    }

    protected void SetOfferTransfersEnabled(bool enabled)
    {
        if (enabled)
        {
            UI.OfferPanel.ItemTakenInitiator += OnOfferPanelInitiatorTaken;
            UI.OfferPanel.ItemTakenResponder += OnOfferPanelRespondentTaken;
            UI.InitiatorInventory.ItemTaken += OnInitiatorInventoryTake;
            UI.RespondentInventory.ItemTaken += OnRespondentInventoryTake;
        }
        else
        {
            UI.OfferPanel.ItemTakenInitiator -= OnOfferPanelInitiatorTaken;
            UI.OfferPanel.ItemTakenResponder -= OnOfferPanelRespondentTaken;
            UI.InitiatorInventory.ItemTaken -= OnInitiatorInventoryTake;
            UI.RespondentInventory.ItemTaken -= OnRespondentInventoryTake;
        }
    }

    protected void ResetInventories(TradeInventoryData initiator, TradeInventoryData respondent)
    {
        UI.InitiatorInventory.UpdateData(initiator);
        UI.RespondentInventory.UpdateData(respondent);
    }

    protected void SetOfferLocked(bool locked)
    {
        UI.OfferPanel.SetLocked(locked);
        UI.InitiatorInventory.SetVisibility(!locked);
        UI.RespondentInventory.SetVisibility(!locked);
    }

    protected void OnOfferPanelInitiatorTaken(CosmeticItem item)
    {
        UI.InitiatorInventory.PutAnywhere(item);
        UI.InitiatorInventory.SetVisibility(true);
        UI.RespondentInventory.SetVisibility(true);
    }

    protected void OnOfferPanelRespondentTaken(CosmeticItem item)
    {
        UI.RespondentInventory.PutAnywhere(item);
        UI.InitiatorInventory.SetVisibility(true);
        UI.RespondentInventory.SetVisibility(true);
    }
    protected void OnInitiatorInventoryTake(CosmeticItem cosmeticItem)
    {
        if (UI.OfferPanel.InitiatorSlot.IsFull)
        {
            var temp = UI.OfferPanel.InitiatorSlot.Item;
            UI.OfferPanel.InitiatorSlot.SetEmpty();
            UI.OfferPanel.SetInitiatorGive(cosmeticItem);
            UI.InitiatorInventory.PutAnywhere(temp);
        }
        else
        {
            UI.OfferPanel.SetInitiatorGive(cosmeticItem);
        }
    }

    protected void OnRespondentInventoryTake(CosmeticItem cosmeticItem)
    {
        if (UI.OfferPanel.ResponderSlot.IsFull)
        {
            var temp = UI.OfferPanel.ResponderSlot.Item;
            UI.OfferPanel.ResponderSlot.SetEmpty();
            UI.OfferPanel.SetInitiatorReceive(cosmeticItem);
            UI.RespondentInventory.PutAnywhere(temp);
        }
        else
        {
            UI.OfferPanel.SetInitiatorReceive(cosmeticItem);
        }
    }

    public void ShowSessionResult(string message)
    {
        UI.Actions.RemoveAll();
        UI.Actions.AddButton(cmdOK, "OK", OnBtnClose);
        UI.Actions.SetButtonInteractable(cmdOK, true);
        UI.InitiatorInventory.SetVisibility(false);
        UI.RespondentInventory.SetVisibility(false);
        UI.OfferPanel.SetLocked(true);
        UI.OfferPanel.SetSessionStatus(message);
    }

    private void OnBtnClose(string _)
    {
        StateBase.InvokeCloseViewRequest();
    }
}
