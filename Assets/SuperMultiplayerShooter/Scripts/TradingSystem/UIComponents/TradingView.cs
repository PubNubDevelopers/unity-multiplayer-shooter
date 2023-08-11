using PubNubUnityShowcase;
using PubNubUnityShowcase.UIComponents;
using System.Threading;
using UnityEngine;

namespace PubNubUnityShowcase.UIComponents
{
    public class TradingView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private OfferPanel offerPanel;
        [SerializeField] private ActionButtonsPanel actionButtons;

        [Header("Trader Components")]
        [SerializeField] private AvatarPanel initiatorAvatar;
        [SerializeField] private TraderInventoryPanel initiatorInventory;

        [Space(10)]

        [SerializeField] private AvatarPanel respondentAvatar;
        [SerializeField] private TraderInventoryPanel respondentInventory;

        [Header("Debug")]
        //[SerializeField] private Text initiatorNameText;
        //[SerializeField] private Text respondentNameText;
        [SerializeField] private TraderData _initiator;
        [SerializeField] private TraderData _respondent;
        private CancellationTokenSource _viewCts;
        private TradingViewStateBase _state;


        /// <summary>
        /// Initialize the view
        /// </summary>
        /// <param name="initData">All needed data and services</param>
        /// <param name="token">Token to cancel all async operations when the view is destroyed </param>
        /// <remarks> The data should include any immutable data (during the whole trading flow) and all the dependency services that this view will need (ex. PNApi instance) </remarks>
        public void Construct(TradingViewData initData, CancellationTokenSource cts)
        {
            _initiator = initData.Initiator;
            _respondent = initData.Respondent;
            _viewCts = cts;

            //Initialize UI Elements
            offerPanel.Construct();
            initiatorInventory.Construct(initData.Services.HatsLibrary, 8);
            respondentInventory.Construct(initData.Services.HatsLibrary, 8);
            initiatorAvatar.Construct(initData.Services.Avatars, initData.Services.HatsLibrary, offerPanel.ResponderSlot, initiatorInventory, _initiator.EquippedCosmetic);
            respondentAvatar.Construct(initData.Services.Avatars, initData.Services.HatsLibrary, offerPanel.InitiatorSlot, respondentInventory, _respondent.EquippedCosmetic);

            //Set Initial State
            initiatorInventory.UpdateData(_initiator.Inventory);
            respondentInventory.UpdateData(_respondent.Inventory);

            initiatorAvatar.SetNickname(_initiator.DisplayName);
            initiatorAvatar.SetHat(_initiator.EquippedCosmetic);
            initiatorAvatar.SetBody(_initiator.PlayerAvatarType);
            initiatorAvatar.SetLookDirection(1);

            respondentAvatar.SetNickname(_respondent.DisplayName);
            respondentAvatar.SetHat(_respondent.EquippedCosmetic);
            respondentAvatar.SetBody(_respondent.PlayerAvatarType);
            respondentAvatar.SetLookDirection(-1);

            //  Check duplicates
            respondentInventory.CheckDuplicates(initiatorInventory.GetCosmetics(), true);
            initiatorInventory.CheckDuplicates(respondentInventory.GetCosmetics(), true);

            if(initData.State == State.initiator)
            {
                //Setup Flow
                UIComponents ui = new UIComponents(offerPanel, initiatorInventory, respondentInventory, actionButtons);
                _state = new TradingViewStateInitiator(initData, ui);
                _state.ApplyState();
                _state.CloseViewRequested += OnSelfClose;
            }

            if (initData.State == State.respondent)
            {
                //Setup Flow
                UIComponents ui = new UIComponents(offerPanel, initiatorInventory, respondentInventory, actionButtons);
                _state = new TradingViewStateRespondent(initData.InitiatorOffer, initData, ui);
                _state.ApplyState();
                _state.CloseViewRequested += OnSelfClose;
            }
        }

        public void OnOpenView()
        {
            Debug.Log($"View Open");
        }

        /// <summary>
        /// Close without any network message ()
        /// </summary>
        public void OnSelfClose()
        {           
            Dispose();
            Destroy(gameObject);
            Debug.Log($"View Closed");
        }

        private void Dispose()
        {
            _viewCts.Cancel();
            _state.Dispose();
        }

        public enum State
        {
            initiator, 
            respondent
        }

        public class UIComponents
        {
            public OfferPanel OfferPanel;
            public TraderInventoryPanel InitiatorInventory;
            public TraderInventoryPanel RespondentInventory;
            public ActionButtonsPanel Actions;

            public UIComponents(OfferPanel offerPanel, TraderInventoryPanel initiatorInventory, TraderInventoryPanel respondentInventory, ActionButtonsPanel actions)
            {
                OfferPanel = offerPanel;
                InitiatorInventory = initiatorInventory;
                RespondentInventory = respondentInventory;
                Actions = actions;
            }
        }

        public class Services 
        {
            public ICosmeticItemLibrary HatsLibrary { get; }
            public IAvatarLibrary Avatars { get; }
            public ITrading Trading { get; }

            public Services(ICosmeticItemLibrary hatsLibrary, IAvatarLibrary avatars, ITrading networkTrading)
            {
                HatsLibrary = hatsLibrary;
                Avatars = avatars;
                Trading = networkTrading;
            }
        }
    }
}