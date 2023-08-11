using PubNubUnityShowcase.UIComponents;
using System;

namespace PubNubUnityShowcase
{
    public abstract class TradingViewStateBase
    {

        private readonly TradeSessionData _sessionData;
        private readonly TradingView.UIComponents _ui;
        private FlowBase _flow;
        protected readonly TradingView.Services _services;

        protected FlowBase Flow { get => _flow; set => _flow = value; }
        protected TradeSessionData SessionData => _sessionData;
        protected TradingView.UIComponents UI => _ui;

        public bool UIElementsInitialized { get; set; }

        public event Action CloseViewRequested;

        protected TradingView.Services Services { get => _services; }
        protected TradingViewStateBase(TradeSessionData sessionData, TradingView.Services services, TradingView.UIComponents ui)
        {
            _sessionData = sessionData;
            _ui = ui;

            _services = services;
        }

        public void Join()
        {
            Services.Trading.JoinSessionAsync(SessionData);
        }

        public void InvokeCloseViewRequest()
        {
            CloseViewRequested?.Invoke();
        }

        private void InvokeCloseViewRequest(string _)
        {
            CloseViewRequested?.Invoke();
        }

        public virtual void Dispose()
        {
            Flow.Unload();
        }

        public void StateSessionComplete(string message, bool autoclose = false)
        {
            Flow.Unload();
            Flow = new FlowSessionClosed(message, SessionData, this, UI, Services);
            Flow.Load();

            if (autoclose)
            {

            }


        }
    }
}