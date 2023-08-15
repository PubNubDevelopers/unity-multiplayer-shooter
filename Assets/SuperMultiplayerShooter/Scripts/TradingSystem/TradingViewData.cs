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
        private readonly TradingView.StateType _mode;

        public TradingViewData(TraderData sellerPlayer, TraderData buyerPlayer, TradingView.Services services, TradingView.StateType mode)
        {
            _initiator = sellerPlayer;
            _respondent = buyerPlayer;
            _services = services;
            _mode = mode;
        }

        public TradingView.StateType State { get => _mode; }
        public TraderData Initiator { get => _initiator; }
        public TraderData Respondent { get => _respondent; }
        public TradeSessionData Session { get; private set; }
        public OfferData InitiatorOffer { get; private set; }
        public TradingView.Services Services { get => _services; }

        /// <summary>
        /// RespondentState
        /// </summary>
        /// <param name="initiatorOffer"></param>
        public void SetStateData(TradeSessionData sessionData, OfferData initiatorOffer)
        {
            InitiatorOffer = initiatorOffer;
            Session = sessionData;
        }
    }
}