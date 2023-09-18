using PubnubApi;
using PubNubUnityShowcase;
using PubNubUnityShowcase.UIComponents;
using System.Threading;
using UnityEngine;

public class MockLauncher : MonoBehaviour
{
    [SerializeField] private TradingService tradingService;

    [Header("Mock Data")]
    [SerializeField] private TradeSessionData mockSession;
    [SerializeField] private OfferData mockOffer;
    [SerializeField] private TradingView view;

    #region UnityEvents (for Buttons setup manually in the Editor)

    public async void OnBtnOpenInitiator()
    {
        var cts = new CancellationTokenSource();
        var viewData = await TradingService.Instance.GetViewDataInitiator(mockSession.Respondent.UserID, cts.Token);
        var view = TradingService.Instance.OpenView(viewData, cts.Token);
    }

    public void OnBtnOpenRespondent()
    {
        var cts = new CancellationTokenSource();
        var viewData = tradingService.GetViewDataRespondent(mockSession, mockOffer, cts.Token);
        tradingService.OpenView(viewData, cts.Token);
    }

    public void OnBtnClose()
    {
        if (view == null)
            return;
        tradingService.CloseView();
    }
    #endregion
}
