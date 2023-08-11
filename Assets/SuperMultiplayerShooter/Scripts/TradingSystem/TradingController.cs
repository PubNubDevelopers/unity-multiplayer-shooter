using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    public class TradingController : ITrading
    {
        private List<ITradeInviteSubscriber> inviteSubscribers;
        private List<ITradeSessionSubscriber> sessionSubscribers;
        private readonly PubNubTrading _network;
        private readonly string _you;

        public bool InLobbyOrMatch => Connector.instance.CurrentRoom != null;
        public bool InSession { get => SessionData.Id > 0; } //todo: may be buggy -> implement session state in session data
        private PubNubTrading Network => _network;
        private string You { get => _you; }
        private TradeSessionData SessionData { get; set; }

        private string DebugTag => $"<color=green>[Trading]</color>";
        private string DebugSession => $"(Session:{SessionData.Id})";
        private ITrading Trading => this;

        private OfferData SentOffer { get; set; }

        public TradingController(string userID)
        {
            _you = userID;

            _network = new PubNubTrading();
            _network.ReceivedInvite += OnReceiveTradeInvite;
            _network.ReceivedInviteResponse += OnReceiveInviteResponse;
            _network.ReceivedOffer += OnReceiveOffer;
            _network.SessionPresenceChanged += OnSessionOccupancyChanged;
            _network.ParticipantGoodbye += OnParticipantGoodbye;

            inviteSubscribers = new List<ITradeInviteSubscriber>();
            sessionSubscribers = new List<ITradeSessionSubscriber>();
        }

        #region ITrading


        async void ITrading.JoinTradingAsync()
        {           
            try
            {
                await Network.SubscribeToTradeInvites();
                Debug.Log($"{DebugTag} Joined Trading.");
            }
            catch (System.Exception e) { Debug.LogError($"{DebugTag} Exception: {e}"); }
        }

        async void ITrading.DisconnectTradingAsync()
        {
            try
            {
                await Network.UnubscribeToTradeInvites();
                Debug.Log($"{DebugTag} Disconnect Trading.");
            }
            catch (System.Exception e) { Debug.LogError($"{DebugTag} Exception: {e}"); }            
        }

        TradeSessionData ITrading.CreateSession(TraderData initiator, TraderData respondent)
        {
            SessionData = new TradeSessionData(GetRandomSessionID(), initiator, respondent);  
            return SessionData;
        }

        async Task ITrading.SendInviteAsync(OfferData offer)
        {
            var invite = new TradeInvite(SessionData, offer);
            await Network.SendTradeInviteAsync(invite);
        }

        async Task ITrading.InviteRespondAsync(TradeInvite invite, InviteResponseData response)
        {
            if (response.WillJoin)
                await Network.SubscribeSession(invite.SessionData);
        }

        async Task ITrading.JoinSessionAsync(TradeSessionData session)
        {
            try
            {
                await Network.SubscribeSession(session);
                SessionData = session;
            }
            catch (System.Exception e)
            {

                throw e;
            }
            Debug.Log($"{DebugTag} ({SessionData.Id}) Joined Session.");
        }

        async Task ITrading.LeaveSessionAsync(LeaveSessionData leaveData)
        {
            var id = SessionData.Id;

            try
            {
                await Network.UnsubscribeSession(SessionData);
                await Network.SendLeaveMessage(SessionData, leaveData);
                SessionData = default;
            }
            catch (System.Exception e)
            {
                throw e;
            }
            Debug.Log($"{DebugTag} ({id}) Left Session.");
        }

        async Task ITrading.SendOfferAsync(OfferData offer)
        {
            SentOffer = await Network.SendOffer(SessionData, offer);
        }

        async Task ITrading.SendCounterOffer(OfferData offer)
        {
            var json = JsonConvert.SerializeObject(offer);

            Debug.Log($"{DebugTag} Counteroffer >>>{json}<<<");
            await Task.CompletedTask;
        }

        void ITrading.SubscribeTradeInvites(ITradeInviteSubscriber subscriber)
        {
            inviteSubscribers.Add(subscriber);
        }

        void ITrading.SubscribeSessionEvents(ITradeSessionSubscriber subscriber)
        {
            sessionSubscribers.Add(subscriber);
        }
        #endregion

        #region Network Event Handlers
        private async void OnReceiveTradeInvite(TradeInvite invite)
        {
            var json = ((IJsonSerializable)invite).RawJson;

            Debug.Log($"{DebugTag} ReceivedTradeInvite: >>>{json}<<<");

            //notify listeners
            foreach (var sub in inviteSubscribers)
                sub.OnTradeInviteReceived(invite);

            InviteResponseData response;

            if (InLobbyOrMatch)
            {
                response = new InviteResponseData(false, false, true);
            }
            else if (InSession)
            {
                response = new InviteResponseData(false, true, false);
            }
            else
            {
                response = new InviteResponseData(true, false, false);

                //note: Auto Join session (it can be also a notification button in the UI)
                await Trading.JoinSessionAsync(invite.SessionData);
            }

            await Network.SendInviteResponse(invite, response);
        }

        private void OnReceiveInviteResponse(InviteResponseData response)
        {
            Debug.Log($"<color=green>[Trading]</color> You received Invite Response");

            foreach (var sub in inviteSubscribers)
            {
                sub.OntradeInviteResponse(response);
            }
        }

        private async void OnReceiveOffer(OfferData offer)
        {
            Debug.Log($"<color=green>[Trading]</color> offer=>>>{((IJsonSerializable)offer).RawJson}");

            string target = SessionData.GetUser(offer.Target);

            switch (offer.State)
            {
                case OfferData.OfferState.open:
                    foreach (var sub in sessionSubscribers)
                        sub.OnCounterOffer(offer);
                    break;
                case OfferData.OfferState.accepted:
                    if (target.Equals(You))
                        await Network.ApplyMetadata(SessionData, offer);

                    foreach (var sub in sessionSubscribers)
                        sub.OnTradingCompleted(offer);

                    break;
                case OfferData.OfferState.rejected:
                    foreach (var sub in sessionSubscribers)
                        sub.OnTradingCompleted(offer);
                    break;
                default:
                    break;
            }
        }

        //public async void OnAcceptedOffer(OfferData offer)
        //{
        //    //Apply data
        //    await Network.ApplyMetadata(SessionData, offer);

        //    foreach (var sub in sessionSubscribers)
        //        sub.OnTradingCompleted(offer);
        //}

        //public void OnReceiveCounterOffer(OfferData offer)
        //{
        //    Debug.Log($"<color=green>[Trading]</color> Received counter offer");
        //}

        //public void OnReceiveRejection(OfferData offer)
        //{
        //    Debug.Log($"<color=green>[Trading]</color> Received Rejection");
        //    Debug.Log($"<color=green>[Trading]</color> You successufully sent offer");
        //    foreach (var sub in sessionSubscribers)
        //        sub.OnCounterOffer(offer);
        //}

        private void OnParticipantGoodbye(LeaveSessionData leaveData)
        {
            //Ignore own events 
            if (leaveData.Participant.UserID.Equals(You))
                return;

            foreach (var sub in sessionSubscribers)
                sub.OnParticipantGoodbye(leaveData);
        }

        private void OnSessionOccupancyChanged(string user, string eventType)
        {
            //Ignore own events 
            if (user.Equals(You))
                return;

            var participant = SessionData.GetParticipantById(user);

            switch (eventType)
            {
                case "join":
                    foreach (var sub in sessionSubscribers)
                        sub.OnParticipantJoined(participant);
                    break;
                case "leave":
                    foreach (var sub in sessionSubscribers)
                        sub.OnLeftUnknownReason(participant);
                    break;
                default:
                    break;
            }
        }
        #endregion

        //Probably it is possible to generate some uid from the time token
        private long GetRandomSessionID()
        {
            return UnityEngine.Random.Range(100000, 999999);
        }
    }
}
