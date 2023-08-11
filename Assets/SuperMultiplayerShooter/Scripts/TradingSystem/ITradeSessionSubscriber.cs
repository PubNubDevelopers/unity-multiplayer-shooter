using PubNubUnityShowcase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITradeSessionSubscriber
{
    void OnParticipantJoined(TraderData participant);

    /// <summary>
    /// Should only happen when the other participant leaves
    /// </summary>
    void OnParticipantGoodbye(LeaveSessionData leaveData);    

    void OnTradingCompleted(OfferData offerData);

    void OnCounterOffer(OfferData offerData);

    void OnLeftUnknownReason(TraderData participant);
}
