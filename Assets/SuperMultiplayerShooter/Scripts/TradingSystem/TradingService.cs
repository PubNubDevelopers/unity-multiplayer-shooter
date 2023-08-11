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

        public ITrading Trading => _tradingController;
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

        //public TradingView OpenViewAsOfferEditor(string targetUser)
        //{
        //    var cts = new CancellationTokenSource();
        //    var data = GetViewDataInitiator(targetUser, cts.Token);
        //    return OpenView(data, cts.Token);
        //}

        //public TradingView OpenViewAsRespondent(TradeSessionData session, OfferData offer)
        //{
        //    var cts = new CancellationTokenSource();
        //    var data = GetViewDataRespondent(session, offer, cts.Token);
        //    return OpenView(data, cts.Token);
        //}
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
        public TradingViewData GetViewDataInitiator(string targetUser, CancellationToken token)
        {
            //Get Initiator data from cache
            TradeInventoryData initiatorInventory = default;
            if (PNManager.pubnubInstance.CachedPlayers.TryGetValue(_connector.GetPubNubObject().GetCurrentUserId(), out var initatorMetadata))
                initiatorInventory = new TradeInventoryData(MetadataNormalization.GetHats(initatorMetadata.Custom));
            else
                Debug.LogError("Player Does Not Exist!");

            var initiator = new TraderData(
                initatorMetadata.Uuid,
                initatorMetadata.Name,
                DataCarrier.chosenCharacter,
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

            TradingView.Services services = new TradingView.Services(CosmeticAssets, AvatarAssets, Trading, token);
            TradingViewData viewData = new TradingViewData(initiator, respondent, services);

            viewData.SetStateData();
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
            TradingViewData viewData = new TradingViewData(session.Initiator, session.Respondent, services);

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
            Trading.DisconnectTradingAsync();
            Trading.UnsubscribeTradeInvites(this);
            _tradingController.Dispose();
        }
    }
}
