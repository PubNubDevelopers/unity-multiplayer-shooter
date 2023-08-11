using PubNubUnityShowcase;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Game objects that can react to Trade invites
    /// </summary>
    /// note: this can be used for Game Controllers, UI, Notifications, SFX etc..
    public interface ITradeInviteSubscriber
    {
        void OnTradeInviteReceived(TradeInvite invite);
        void OnTradeInviteWithdrawn(TradeInvite invite);
        void OntradeInviteResponse(InviteResponseData response);
    }
}