namespace PubNubUnityShowcase.UIComponents
{
    public class FlowInitialOfferReceived : FlowBase
    {
        private readonly OfferData _initialData;

        public FlowInitialOfferReceived(OfferData data, TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
            _initialData = data;
        }

        public override void Load()
        {
            FillOfferPanelFromInventories(_initialData);
        }

        public override void Unload()
        {

        }
    }
}