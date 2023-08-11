using PubNubUnityShowcase.ScriptableObjects;
using PubNubUnityShowcase.UIComponents;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    public class TradingService : MonoBehaviour,
        ITradeInviteSubscriber
    {
        public static TradingService Instance; //To work as a Singleton

        [Header("Components")]
        [SerializeField] private Canvas canvas;                         //The canvas where the view will be instantiated

        [Header("Assets")]
        [SerializeField] private CosmeticsLibrary assets;               //Assets
        [SerializeField] private TradingView tradingViewPrefab;         //View Prefab   

        
        private CancellationTokenSource cts;
        private TradingView view;
        private Connector _connector;
        private TradingController _tradingController;

        private ITrading Trading => _tradingController;
        private IAvatarLibrary AvatarAssets => assets;
        private ICosmeticItemLibrary CosmeticAssets => assets;

        public TradingService InitializeAsSingleton(string name, bool persistent)
        {
            if (Instance == null)
            {
                Instance = this;
                gameObject.name = "manager: " + name;
            }

            else if (Instance != this)
                Destroy(gameObject);
            if (persistent == true)
            {
                gameObject.transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            return Instance;
        }

        public TradingView OpenViewAsOfferEditor(string targetUser)
        {
            var data = GetViewDataInitiator(targetUser);
            return SpawnView(data);
        }

        public TradingView OpenViewAsRespondent(TradeSessionData session, OfferData offer)
        {
            var data = GetViewDataRespondent(session, offer);
            return SpawnView(data);
        }

        private async void Start()
        {
            InitializeAsSingleton("TradingService", true);
            await Task.Delay(1000); //TODO: workaround just to make sure PN is initialized
            _connector = Connector.instance;
            _tradingController = new TradingController(_connector.GetPubNubObject().GetCurrentUserId());

            Trading.SubscribeTradeInvites(this);
            Trading.JoinTradingAsync();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private TradingView SpawnView(TradingViewData viewData)
        {
            if (view != null)
                return null;

            //Spawn the view
            view = Instantiate(tradingViewPrefab, canvas.transform);
            view.Construct(viewData, cts);
            view.OnOpenView();

            return view;
        }

        private TradingViewData GetViewDataInitiator(string targetUser)
        {
            cts = new CancellationTokenSource();

            //Get Initiator data from cache
            TradeInventoryData initiatorInventory = default;
            if (PNManager.pubnubInstance.CachedPlayers.TryGetValue(_connector.GetPubNubObject().GetCurrentUserId(), out var initatorMetadata))
                initiatorInventory = new TradeInventoryData(MetadataNormalization.GetHats(initatorMetadata.Custom));
            else
                Debug.LogError("Player Does Not Exist!");

            var initiator = new TraderData(
                initatorMetadata.Uuid,
                initatorMetadata.Name,
                0,//TODO: find a way to get this 
                initiatorInventory,
                initiatorInventory.CosmeticItems[0]); //TODO: find a way to get this 

            //Get Respondent data from cache
            TradeInventoryData respondentInventory = default;
            if (PNManager.pubnubInstance.CachedPlayers.TryGetValue(targetUser, out var respondentMetadata))
                respondentInventory = new TradeInventoryData(MetadataNormalization.GetHats(respondentMetadata.Custom));
            else
                Debug.LogError("Player Does Not Exist!");

            var respondent = new TraderData(
                respondentMetadata.Uuid,
                respondentMetadata.Name,
                1,//TODO: find a way to get this 
                respondentInventory,
                respondentInventory.CosmeticItems[0]);//TODO: find a way to get this 


            TradingView.Services services = new TradingView.Services(CosmeticAssets, AvatarAssets, Trading);


            TradingViewData viewData = new TradingViewData(initiator, respondent, services, cts.Token);

            viewData.SetStateData();
            return viewData;
        }

        private TradingViewData GetViewDataRespondent(TradeSessionData session, OfferData offer)
        {
            cts = new CancellationTokenSource();

            TradingView.Services services = new TradingView.Services(CosmeticAssets, AvatarAssets, Trading);

            cts = new CancellationTokenSource();
            TradingViewData viewData = new TradingViewData(session.Initiator, session.Respondent, services, cts.Token);

            viewData.SetStateData(session, offer);

            return viewData;
        }

        public void CloseView()
        {
            if (view == null)
                return;

            Dispose();
            Destroy(view.gameObject);
        }

        #region Integrate into UI
        void ITradeInviteSubscriber.OnTradeInviteReceived(TradeInvite invite)
        {
            Debug.Log($"TradeInviteReceived!!! ch= {invite.SessionData.Channel}");
            OpenViewAsRespondent(invite.SessionData, invite.OfferData);
        }

        void ITradeInviteSubscriber.OnTradeInviteWithdrawn(TradeInvite invite)
        {
            //View should be also subscribed to this event and will handle closing.
            //also notification icons for trading can use this
        }

        void ITradeInviteSubscriber.OntradeInviteResponse(InviteResponseData response)
        {
            //Debug.Log($"Respondent: json={((IJsonSerializable)response).RawJson}");
        }

        #endregion

        private void Dispose()
        {
            cts?.Cancel();
            view = null;
            Trading.DisconnectTradingAsync();
            Trading.UnsubscribeTradeInvites(this);
            _tradingController.Dispose();
        }
    }
}
