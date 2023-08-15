using PubNubUnityShowcase.ScriptableObjects;
using PubNubUnityShowcase.UIComponents;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Trading Service (Singleton)
    /// <para>To Integrate into any UI just implement this interface and subscribe with</para>
    /// TradingService.Instance.Trading.SubscribeTradeInvites(this);<br/>
    /// TradingService.Instance.Trading.SubscribeSessionEvents(this);
    /// </summary>

    public class TradingService : MonoBehaviour,
        ITradeInviteSubscriber
    {
        public static TradingService Instance; //To work as a Singleton

        [Header("Components")]
        [SerializeField] private Canvas canvas;                         //The canvas where the view will be instantiated

        [Header("Assets")]
        [SerializeField] private CosmeticsLibrary assets;               //Assets
        [SerializeField] private TradingView tradingViewPrefab;         //View Prefab   


        //private CancellationTokenSource cts;
        private TradingView view;
        private Connector _connector;
        private TradingController _tradingController;
        private TraderDataCached _cachedTraders;

        public ITrading Trading => _tradingController;
        public ITradingDatastore Datastore {get; private set;}
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

        public void CloseView()
        {
            if (view == null)
                return;

            Dispose();
            Destroy(view.gameObject);
        }

        private async void Start()
        {
            InitializeAsSingleton("TradingService", true);

            var cts = new CancellationTokenSource(10000);
            try
            {
                while (_connector == null)
                {
                    _connector = Connector.instance;
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100);
                    await Task.Yield();
                }
                
                _tradingController = new TradingController(_connector.GetPubNubObject().GetCurrentUserId());
                Trading.SubscribeTradeInvites(this);
                Trading.JoinTradingAsync();

                Datastore = _tradingController; //requests are done every time view is opened
                //Datastore = new TraderDataCached(); //data is taken from cache
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
            {
                Debug.LogError($"{typeof(Connector).Name} didn't initialize for 10+ seconds.");
                gameObject.SetActive(false);
            }        
        }

        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Opens the trading window
        /// </summary>
        /// <param name="viewData"></param>
        /// <param name="token">if this token is cancelled it should cancel all async operations of the view </param>
        /// <remarks>Not every async method in the view uses tokens tho, but they can be added as needed</remarks>
        public TradingView OpenView(TradingViewData viewData, CancellationToken token)
        {
            if (view != null)
                return null;

            //Spawn the view
            view = Instantiate(tradingViewPrefab, canvas.transform);
            view.Construct(viewData, token);
            view.OnOpenView();
            return view;
        }

        /// <summary>
        /// Helper to get proper data for the view
        /// </summary>
        public async Task<TradingViewData> GetViewDataInitiator(string targetUser, CancellationToken token)
        {
            TradingView.Services services = new TradingView.Services(CosmeticAssets, AvatarAssets, Trading, token);

            var initiator = await Datastore.GetTraderData(_connector.GetPubNubObject().GetCurrentUserId());
            var respondent = await Datastore.GetTraderData(targetUser);

            TradingViewData viewData = new TradingViewData(initiator, respondent, services, TradingView.StateType.initiator);

            return viewData;
        }

        /// <summary>
        /// Helper to get proper data for the view
        /// <para>To Integrate into any UI just implement this interface and subscribe with</para>
        /// TradingService.Instance.Trading.SubscribeTradeInvites(this);<br/>
        /// TradingService.Instance.Trading.SubscribeSessionEvents(this);
        /// </summary>
        public TradingViewData GetViewDataRespondent(TradeSessionData session, OfferData offer, CancellationToken token)
        {
            TradingView.Services services = new TradingView.Services(CosmeticAssets, AvatarAssets, Trading, token);
            TradingViewData viewData = new TradingViewData(session.Initiator, session.Respondent, services, TradingView.StateType.respondent);

            viewData.SetStateData(session, offer);

            return viewData;
        }

        //To Integrate into any UI just implement this interface and subscribe with 
        //TradingService.Instance.Trading.SubscribeSessionEvents(this);
        //TradingService.Instance.Trading.SubscribeTradeInvites(this);

        #region ITradeInviteSubscriber
        void ITradeInviteSubscriber.OnTradeInviteReceived(TradeInvite invite)
        {      
            var cts = new CancellationTokenSource();
            var viewData = GetViewDataRespondent(invite.SessionData, invite.OfferData, cts.Token);
            OpenView(viewData, cts.Token);
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
            view = null;
            Trading?.DisconnectTradingAsync();
            Trading?.UnsubscribeTradeInvites(this);
            _tradingController.Dispose();
        }
    }
}
