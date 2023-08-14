using PubNubUnityShowcase.UIComponents;
using System.Threading;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Initialization data for <see cref="TradingView"/>
    /// </summary>
    public class TradingViewData
    {
        private readonly TraderData _initiator;
        private readonly TraderData _respondent;
        private readonly TradingView.Services _services;

        public TradingViewData(TraderData sellerPlayer, TraderData buyerPlayer, TradingView.Services services)
        {
            _initiator = sellerPlayer;
            _respondent = buyerPlayer;
            _services = services;
        }

        public TradingView.StateType State { get; private set; }
        public TraderData Initiator { get => _initiator; }
        public TraderData Respondent { get => _respondent; }
        public TradeSessionData Session { get; private set; }
        public OfferData InitiatorOffer { get; private set; }
        public TradingView.Services Services { get => _services; }

        /// <summary>
        /// Initiator State
        /// </summary>
        public void SetStateData()
        {
            State = TradingView.StateType.initiator;
        }

        /// <summary>
        /// RespondentState
        /// </summary>
        /// <param name="offer"></param>
        public void SetStateData(TradeSessionData session, OfferData offer)
        {
            State = TradingView.StateType.respondent;
            InitiatorOffer = offer;
            Session = session;
        }
    }
}