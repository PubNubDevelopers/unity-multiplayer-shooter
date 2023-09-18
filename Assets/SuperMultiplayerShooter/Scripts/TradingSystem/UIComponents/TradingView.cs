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
        private CancellationToken _viewToken;
        private TradingViewStateBase _state;

        private TradingViewStateBase State => _state;

        /// <summary>
        /// Initialize the view
        /// </summary>
        /// <param name="initData">All needed data and services</param>
        /// <param name="token">Token to cancel all async operations when the view is destroyed </param>
        /// <remarks> The data should include any immutable data (during the whole trading flow) and all the dependency services that this view will need (ex. PNApi instance) </remarks>
        public void Construct(TradingViewData initData, CancellationToken viewToken)
        {
            _viewToken = viewToken;

            if (initData.State == StateType.initiator)
            {
                //Setup Flow
                UIComponents ui = new UIComponents(offerPanel, initiatorInventory, respondentInventory, initiatorAvatar, respondentAvatar, actionButtons);
                var session = initData.Services.Trading.GenerateSessionData(initData.Initiator, initData.Respondent);
                _state = new TradingViewStateInitiator(session, initData.Services, ui);
                _state.Join();
                _state.CloseViewRequested += OnSelfClose;
            }

            if (initData.State == StateType.respondent)
            {
                //Setup Flow
                UIComponents ui = new UIComponents(offerPanel, initiatorInventory, respondentInventory, initiatorAvatar, respondentAvatar, actionButtons);
                _state = new TradingViewStateRespondent(initData.Session, initData.InitiatorOffer, initData.Services, ui);
                _state.CloseViewRequested += OnSelfClose;
            }
        }

        public void OnOpenView()
        {
            //Debug.Log($"View Open");
        }

        /// <summary>
        /// Close without any network message ()
        /// </summary>
        public void OnSelfClose()
        {
            Dispose();
            Destroy(gameObject);
            //Debug.Log($"View Closed");
        }

        private void Dispose()
        {
            _state.Dispose();
        }

        public enum StateType
        {
            initiator,
            respondent
        }

        public class UIComponents
        {
            public OfferPanel OfferPanel;
            public TraderInventoryPanel InventoryInitiator;
            public TraderInventoryPanel InventoryRespondent;
            public AvatarPanel AvatarInitiator;
            public AvatarPanel AvatarRespondent;
            public ActionButtonsPanel Actions;

            public UIComponents(
                OfferPanel offerPanel,
                TraderInventoryPanel initiatorInventory,
                TraderInventoryPanel respondentInventory,               
                AvatarPanel avatarInitiator,
                AvatarPanel avatarRespondent,
                ActionButtonsPanel actions)
            {
                OfferPanel = offerPanel;
                InventoryInitiator = initiatorInventory;
                InventoryRespondent = respondentInventory;
                Actions = actions;
                AvatarInitiator = avatarInitiator;
                AvatarRespondent = avatarRespondent;
            }
        }

        public class Services
        {
            public ICosmeticItemLibrary HatsLibrary { get; }
            public IAvatarLibrary Avatars { get; }
            public ITrading Trading { get; }
            public CancellationToken TokenView { get; } 

            public Services(ICosmeticItemLibrary hatsLibrary, IAvatarLibrary avatars, ITrading networkTrading, CancellationToken tokenView)
            {
                HatsLibrary = hatsLibrary;
                Avatars = avatars;
                Trading = networkTrading;
                TokenView = tokenView;
            }
        }
    }
}