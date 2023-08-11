using PubNubUnityShowcase;
using PubNubUnityShowcase.UIComponents;
using UnityEngine;

public class MockLauncher : MonoBehaviour
{
    [SerializeField] private TradingService tradingService;

    [Header("Mock Data")]
    [SerializeField] private TradeSessionData mockSession;
    [SerializeField] private OfferData mockOffer;
    [SerializeField] private TradingView view;

    #region UnityEvents (for Buttons setup manually in the Editor)

    public void OnBtnOpenInitiator()
    {
        tradingService.OpenViewAsOfferEditor(mockSession.Respondent.UserID);
    }

    public void OnBtnOpenRespondent()
    {
        tradingService.OpenViewAsRespondent(mockSession, mockOffer);
    }

    public void OnBtnClose()
    {
        if (view == null)
            return;
        tradingService.CloseView();
    }
    #endregion
}
