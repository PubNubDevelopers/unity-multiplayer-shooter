namespace PubNubUnityShowcase.UIComponents
{
    public class FlowSessionClosed : FlowBase
    {
        private string _message;

        public FlowSessionClosed(string message, TradeSessionData sessionData, TradingViewStateBase stateBase, TradingView.UIComponents ui, TradingView.Services services) : base(sessionData, stateBase, ui, services)
        {
            _message = message;
        }

        public override void Load()
        {
            UI.Actions.RemoveAll();
            UI.Actions.AddButton(cmdOK,"OK", OnBtnOK);
            UI.Actions.SetButtonInteractable(cmdOK, true);

            InventoriesVisibility(false);

            OfferSetLocked(true);
            UI.OfferPanel.SetSessionStatus(_message);
        }

        public override void Unload()
        {
            
        }

        private void OnBtnOK(string _)
        {
            StateBase.InvokeCloseViewRequest();
        }
    }
}
