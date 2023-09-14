using PubnubApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using Visyde;

namespace PubNubUnityShowcase
{
    public class TradingController :
        ITrading,
        ITradingDatastore,
        IDisposable

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

        /// <remarks>Name, avatarType and equipped are still taken from cache until properly implemented </remarks>
        async Task<TraderData> ITradingDatastore.GetTraderData(string traderID)
        {
            TradeInventoryData inventory = await _network.GetTraderInventory(traderID);

            if (PNManager.pubnubInstance.CachedPlayers.TryGetValue(traderID, out var metadata))
                inventory = new TradeInventoryData(MetadataNormalization.GetHats(metadata.Custom));
            int chosenCharacter = 0;
            if (metadata.Custom.ContainsKey("chosen_character"))
            {
                chosenCharacter = Int32.Parse(metadata.Custom["chosen_character"].ToString());               
            }
            //Legacy Default situations
            else
            {
                metadata.Custom.Add("chosen_character", 0); // Defaults to the first character.
                PNManager.pubnubInstance.CachedPlayers[traderID].Custom = metadata.Custom;
            }
            return new TraderData(
                traderID,
                metadata.Name,
                chosenCharacter,
                inventory,
                inventory.CosmeticItems[0]); // using the first hat in the player's inventory, not their selected hat.
        }

        async void ITrading.JoinTradingAsync()
        {
            try
            {
                await Network.SubscribeToTradeInvites();
                Debug.Log($"{DebugTag} Joined Trading.");
            }
            catch (System.Exception e) { Debug.LogError($"{DebugTag} Exception: {e}"); }
        }

        async Task ITrading.DisconnectTradingAsync()
        {
            try
            {
                await Network.UnubscribeToTradeInvites();
                if (InSession)
                    await Network.UnsubscribeSession(SessionData);
                Debug.Log($"{DebugTag} Disconnect Trading.");
            }
            catch (System.Exception e) { Debug.LogError($"{DebugTag} Exception: {e}"); }
        }

        TradeSessionData ITrading.GenerateSessionData(TraderData initiator, TraderData respondent)
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
            catch (Exception e)
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
            catch (Exception e)
            {
                throw e;
            }
            Debug.Log($"{DebugTag} ({id}) Left Session.");
        }

        async Task ITrading.SendOfferAsync(OfferData offer)
        {
            SentOffer = await Network.SendOffer(SessionData, offer);
        }

        void ITrading.SubscribeTradeInvites(ITradeInviteSubscriber subscriber)
        {
            inviteSubscribers.Add(subscriber);
            Debug.Log($"{DebugTag} : subscribed={subscriber.GetType().Name} subs={inviteSubscribers.Count}");
        }

        void ITrading.SubscribeSessionEvents(ITradeSessionSubscriber subscriber)
        {
            sessionSubscribers.Add(subscriber);
            Debug.Log($"{DebugTag} : subscribed={subscriber.GetType().Name} subs={sessionSubscribers.Count}");
        }

        void ITrading.UnsubscribeTradeInvites(ITradeInviteSubscriber subscriber)
        {
            inviteSubscribers.Remove(subscriber);
        }

        void ITrading.UnsubscribeSessionEvents(ITradeSessionSubscriber subscriber)
        {
            sessionSubscribers.Remove(subscriber);
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

            //notify listeners
            foreach (var sub in inviteSubscribers)
                sub.OnTradeInviteReceived(invite);
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
                    if (target.Equals(You))
                    {
                        foreach (var sub in sessionSubscribers)
                            sub.OnCounterOffer(offer);
                    }
                    break;
                case OfferData.OfferState.accepted:
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

        private void OnParticipantGoodbye(LeaveSessionData leaveData)
        {
            //Ignore own events 
            if (leaveData.Participant.UserID.Equals(You))
                return;

            foreach (var sub in sessionSubscribers)
                sub.OnParticipantGoodbyeAsync(leaveData);
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

        public async void Dispose()
        {
            await ((ITrading)this).DisconnectTradingAsync();


            Network.Dispose();
        }
    }
}
