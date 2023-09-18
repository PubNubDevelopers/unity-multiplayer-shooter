using System.Threading.Tasks;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Trading Controller
    /// </summary>
    public interface ITrading
    {
        /// <remarks> To receive trading Invites </remarks>
        void JoinTradingAsync();

        /// <remarks> To stop receiving trading Invites </remarks>
        Task DisconnectTradingAsync();

        /// <summary>
        /// Initialize session as Initiator (Local only)
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="respondent"></param>
        TradeSessionData GenerateSessionData(TraderData initiator, TraderData respondent);

        /// <summary>
        /// Join Session as Respondent
        /// </summary>
        Task JoinSessionAsync(TradeSessionData session);

        /// <summary>
        /// Announce the session to the respondent (with offer)
        /// </summary>
        /// <param name="maxWait">Milliseconds</param>
        Task SendInviteAsync(OfferData offer);

        Task InviteRespondAsync(TradeInvite invite, InviteResponseData response);

        Task LeaveSessionAsync(LeaveSessionData leaveData);

        Task SendOfferAsync(OfferData offer);

        /// <remarks>Don't forget to unsubscribe!</remarks>
        void SubscribeTradeInvites(ITradeInviteSubscriber subscriber);

        /// <remarks>Don't forget to unsubscribe!</remarks>
        void SubscribeSessionEvents(ITradeSessionSubscriber subscriber);

        void UnsubscribeTradeInvites(ITradeInviteSubscriber subscriber);
        void UnsubscribeSessionEvents(ITradeSessionSubscriber subscriber);
    }
}
