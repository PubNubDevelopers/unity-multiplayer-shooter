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
        private readonly CancellationToken _token;

        public TradingViewData(TraderData sellerPlayer, TraderData buyerPlayer, TradingView.Services services, CancellationToken token)
        {
            _initiator = sellerPlayer;
            _respondent = buyerPlayer;
            _services = services;
            _token = token;
        }

        public TradingView.State State { get; private set; }
        public TraderData Initiator { get => _initiator; }
        public TraderData Respondent { get => _respondent; }
        public OfferData InitiatorOffer { get; private set; }
        public TradingView.Services Services { get => _services; }

        /// <summary>
        /// Initiator State
        /// </summary>
        public void SetStateData()
        {
            State = TradingView.State.initiator;
        }

        /// <summary>
        /// RespondentState
        /// </summary>
        /// <param name="offer"></param>
        public void SetStateData(OfferData offer)
        {
            State = TradingView.State.respondent;
            InitiatorOffer = offer;
        }
    }
}