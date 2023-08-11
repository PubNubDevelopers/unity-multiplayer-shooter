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


    protected void InitializeUIElements()
    {
        //Initialize UI Elements
        UI.OfferPanel.Construct();
        UI.InventoryInitiator.Construct(Services.HatsLibrary, 8);
        UI.InventoryRespondent.Construct(Services.HatsLibrary, 8);
        UI.AvatarInitiator.Construct(Services.Avatars, Services.HatsLibrary, UI.OfferPanel.ResponderSlot, UI.InventoryInitiator, SessionData.Initiator.EquippedCosmetic);
        UI.AvatarRespondent.Construct(Services.Avatars, Services.HatsLibrary, UI.OfferPanel.InitiatorSlot, UI.InventoryRespondent, SessionData.Respondent.EquippedCosmetic);
    }

    protected void RefreshAvatars(TraderData initiator, TraderData respondent)
    {
        UI.AvatarInitiator.SetNickname(initiator.DisplayName);
        UI.AvatarInitiator.SetHat(initiator.EquippedCosmetic);
        UI.AvatarInitiator.SetBody(initiator.PlayerAvatarType);
        UI.AvatarInitiator.SetLookDirection(1);

        UI.AvatarRespondent.SetNickname(respondent.DisplayName);
        UI.AvatarRespondent.SetHat(respondent.EquippedCosmetic);
        UI.AvatarRespondent.SetBody(respondent.PlayerAvatarType);
        UI.AvatarRespondent.SetLookDirection(-1);
    }

    protected void RefreshInventories(TradeInventoryData initiator, TradeInventoryData respondent)
    {
        UI.InventoryInitiator.UpdateData(initiator);
        UI.InventoryRespondent.UpdateData(respondent);
    }

    protected void DisableDuplicateItems()
    {
        //Check duplicates
        UI.InventoryRespondent.CheckDuplicates(UI.InventoryInitiator.GetCosmetics(), true);
        UI.InventoryInitiator.CheckDuplicates(UI.InventoryRespondent.GetCosmetics(), true);
    }

    protected void FillOfferPanelFromInventories(OfferData offerData)
    {
        //Fill offer panel
        UI.InventoryInitiator.RemoveItem(Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorGives));
        UI.OfferPanel.InitiatorSlot.SetItem(Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorGives));

        UI.InventoryRespondent.RemoveItem(Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorReceives));
        UI.OfferPanel.ResponderSlot.SetItem(Services.HatsLibrary.GetCosmeticItem(offerData.InitiatorReceives));
    }

    protected void SetOfferTransfersEnabled(bool enabled)
    {
        if (enabled)
        {
            UI.OfferPanel.ItemTakenInitiator += OnOfferPanelInitiatorTaken;
            UI.OfferPanel.ItemTakenResponder += OnOfferPanelRespondentTaken;
            UI.InventoryInitiator.ItemTaken += OnInitiatorInventoryTake;
            UI.InventoryRespondent.ItemTaken += OnRespondentInventoryTake;
        }
        else
        {
            UI.OfferPanel.ItemTakenInitiator -= OnOfferPanelInitiatorTaken;
            UI.OfferPanel.ItemTakenResponder -= OnOfferPanelRespondentTaken;
            UI.InventoryInitiator.ItemTaken -= OnInitiatorInventoryTake;
            UI.InventoryRespondent.ItemTaken -= OnRespondentInventoryTake;
        }
    }

    protected void SetOfferLocked(bool locked)
    {
        UI.OfferPanel.SetLocked(locked);
        UI.InventoryInitiator.SetVisibility(!locked);
        UI.InventoryRespondent.SetVisibility(!locked);
    }

    protected void OnOfferPanelInitiatorTaken(CosmeticItem item)
    {
        UI.InventoryInitiator.PutAnywhere(item);
        UI.InventoryInitiator.SetVisibility(true);
        UI.InventoryRespondent.SetVisibility(true);
    }

    protected void OnOfferPanelRespondentTaken(CosmeticItem item)
    {
        UI.InventoryRespondent.PutAnywhere(item);
        UI.InventoryInitiator.SetVisibility(true);
        UI.InventoryRespondent.SetVisibility(true);
    }
    protected void OnInitiatorInventoryTake(CosmeticItem cosmeticItem)
    {
        if (UI.OfferPanel.InitiatorSlot.IsFull)
        {
            var temp = UI.OfferPanel.InitiatorSlot.Item;
            UI.OfferPanel.InitiatorSlot.SetEmpty();
            UI.OfferPanel.SetInitiatorGive(cosmeticItem);
            UI.InventoryInitiator.PutAnywhere(temp);
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
            UI.InventoryRespondent.PutAnywhere(temp);
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
        UI.InventoryInitiator.SetVisibility(false);
        UI.InventoryRespondent.SetVisibility(false);
        UI.OfferPanel.SetLocked(true);
        UI.OfferPanel.SetSessionStatus(message);
    }

    private void OnBtnClose(string _)
    {
        StateBase.InvokeCloseViewRequest();
    }
}
