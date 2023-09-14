using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// Requests and listeners (PubNubAPI)
    /// </summary>
    public class PubNubTrading : IDisposable
    {
        public static string TradingPreffix => "trading"; //Trading channel prefix
        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }
        public PubNubTrading()
        {
            PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence += OnPnPresence;
            PNManager.pubnubInstance.onPubNubObject += OnPnObject;
            PNManager.pubnubInstance.onPubNubStatus += OnPnStatus;
        }

        private UserId ThisUser => pubnub.GetCurrentUserId();
        private Dictionary<string, UserMetadata> DatastoreUserMetadata => PNManager.pubnubInstance.CachedPlayers;
        private string DebugTag => $"<color=red>[Network]</color>";

        public event Action<OfferData> ReceivedOffer;
        public event Action<TradeInvite> ReceivedInvite;
        public event Action<InviteResponseData> ReceivedInviteResponse;
        public event Action<LeaveSessionData> ParticipantGoodbye;   //If left with explicid SendLeaveMessage()
        public event Action<string> CommanReceived;
        public event Action<string, string> SessionPresenceChanged; //uuid, event

        #region PubNub Requests
        public async Task SendTradeInviteAsync(TradeInvite invite)
        {
            var respInbox = GetInbox(invite.SessionData.Respondent.UserID);

            await pubnub.Publish()
                .Message(invite)
                .Channel(respInbox)
                .Meta(MessageNormalilzation.GetMeta<TradeInvite>())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task SendInviteResponse(TradeInvite invite, InviteResponseData response)
        {
            await pubnub.Publish()
                .Message(response)
                .Channel(invite.RSVPChannel)
                .Meta(MessageNormalilzation.GetMeta<InviteResponseData>())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task<OfferData> SendOffer(TradeSessionData session, OfferData offer)
        {
            await pubnub.Publish()
                .Message(offer)
                .Channel(session.Channel)
                .Meta(MessageNormalilzation.GetMeta<OfferData>())
                .ShouldStore(true)
                .ExecuteAsync();

            return offer;
        }

        public async Task SendLeaveMessage(TradeSessionData session, LeaveSessionData leaveData)
        {
            await pubnub.Publish()
                .Message(leaveData)
                .Channel(session.Channel)
                .Meta(MessageNormalilzation.GetMeta<LeaveSessionData>())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task SendCommand(TradeSessionData session, string cmd)
        {
            await pubnub.Publish()
                .Message(cmd)
                .Channel(session.Channel)
                .Meta(MessageNormalilzation.GetCommandMeta())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task<string> SubscribeToTradeInvites()
        {
            try
            {
                pubnub.Subscribe<string>()
                    .Channels(new[] { GetInbox(ThisUser) })
                    .Execute();

                await Task.Delay(100);
                //Debug.Log($"{DebugTag} Subscribed ch={GetInbox(ThisUser)}");

                return GetInbox(ThisUser);
            }
            catch (Exception e) { throw e; }
        }

        public async Task UnubscribeToTradeInvites()
        {
            try
            {
                pubnub.Unsubscribe<string>()
                    .Channels(new[] { GetInbox(ThisUser) })
                    .Execute();

                await Task.Delay(100);
                //Debug.Log($"{DebugTag} Unsubscribed ch={GetInbox(ThisUser)}");
            }
            catch (Exception e) { throw e; }
        }

        public async Task SubscribeSession(TradeSessionData session)
        {
            pubnub.Subscribe<string>()
                .Channels(new[] { session.Channel })
                .WithPresence()
                .Execute();

            await Task.Delay(100);
        }

        public async Task UnsubscribeSession(TradeSessionData session)
        {
            pubnub.Unsubscribe<string>()
                .Channels(new[] { session.Channel, $"{session.Channel}-pnpres" })
                .Execute();

            await Task.Delay(100);
            Debug.Log($"<color=red>[Network]</color> Session({session.Id}): unsubscribe ch={session.Channel}-pnpres");
        }

        public async Task<TradeInventoryData> GetTraderInventory(UserId user)
        {
            try
            {
                var response = await pubnub.GetUuidMetadata()
                    .Uuid(user)
                    .IncludeCustom(true)
                    .ExecuteAsync();

                if (response == null)
                    throw new NullReferenceException(nameof(response));

                TradeInventoryData traderData = new TradeInventoryData(MetadataNormalization.GetHats(response.Result.Custom));
                return traderData;
            }
            catch (Exception e)
            {
                Debug.Log($"{DebugTag} (GetInventory): FAILED! >>{e}");
                return TradeInventoryData.GetEmpty();
            }
            finally
            {
                Debug.Log($"{DebugTag} (GetInventory): user={user}");
            }
        }

        public async Task ApplyMetadata(TradeSessionData session, OfferData offer)
        {
            try
            {        
                DatastoreUserMetadata.TryGetValue(session.Initiator.UserID, out var initiatorCurrent);
                DatastoreUserMetadata.TryGetValue(session.Respondent.UserID, out var respondentCurrent);
                MetadataNormalization.ReplaceHats(session.Initiator.UserID, initiatorCurrent.Custom, offer.InitiatorGives, offer.InitiatorReceives);
                MetadataNormalization.ReplaceHats(session.Respondent.UserID, respondentCurrent.Custom, offer.InitiatorReceives, offer.InitiatorGives);

                await PNManager.pubnubInstance.UpdateUserMetadata(session.Initiator.UserID, initiatorCurrent.Name, PNManager.pubnubInstance.CachedPlayers[session.Initiator.UserID].Custom);                                       
                await PNManager.pubnubInstance.UpdateUserMetadata(session.Respondent.UserID, respondentCurrent.Name, PNManager.pubnubInstance.CachedPlayers[session.Respondent.UserID].Custom);                  
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region PubNub EventHandlers
        private void OnPnMessage(PNMessageResult<object> result)
        {
            if (result == null)
                return;

            //filter only trading messages
            if (!result.Channel.StartsWith(TradingPreffix))
                return;

            string json = JsonConvert.SerializeObject(result.Message);

            if (result.UserMetadata != null && result.UserMetadata is Dictionary<string, object>)
            {
                var meta = (Dictionary<string, object>)result.UserMetadata;
                string typeKey = meta[MessageNormalilzation.TYPE_KEY] as string;

                if (meta[MessageNormalilzation.TYPE_KEY].Equals(MessageNormalilzation.COMMAND_KEY))
                {
                    OnMsgPayloadCommand(result.Message.ToString());
                }
                else
                {
                    if (typeKey.Equals(nameof(TradeInvite)))
                        OnMsgPayloadTradeInvite(MessageNormalilzation.GetPayload<TradeInvite>(result.Message));

                    if (typeKey.Equals(nameof(OfferData)))
                        OnMsgPayloadOffer(MessageNormalilzation.GetPayload<OfferData>(result.Message));

                    if (typeKey.Equals(nameof(InviteResponseData)))
                        OnMsgPayloadInviteResponse(MessageNormalilzation.GetPayload<InviteResponseData>(result.Message));

                    if (typeKey.Equals(nameof(LeaveSessionData)))
                        ParticipantGoodbye?.Invoke(MessageNormalilzation.GetPayload<LeaveSessionData>(result.Message));
                }
            }
        }

        private void OnPnPresence(PNPresenceEventResult result)
        {
            if (!result.Channel.StartsWith(TradingPreffix))
                return;

            //it is safe to assume that only the current session channel will receive presence events so no need to check the channel
            SessionPresenceChanged?.Invoke(result.Uuid, result.Event);
        }

        private void OnPnObject(PNObjectEventResult result)
        {
            Debug.Log($"{DebugTag} MetaUpdate event received: {result.Type}");
        }

        private void OnPnStatus(PNStatus status)
        {
            try
            {
                if (status != null)
                {
                    if (status.Operation == PNOperationType.PNSubscribeOperation)
                    {
                        var allTrading = new List<string>();
                        foreach (var ch in status.AffectedChannels)
                        {
                            if (ch.StartsWith(TradingPreffix))
                                allTrading.Add(ch);
                            OnStatusSubscribeAnyTrading(allTrading);
                        }
                    }

                    if (status.Operation == PNOperationType.PNUnsubscribeOperation)
                    {
                        var allTrading = new List<string>();
                        foreach (var ch in status.AffectedChannels)
                        {
                            if (ch.StartsWith(TradingPreffix))
                                allTrading.Add(ch);
                            OnStatusUnsubscribeAnyTrading(allTrading);
                        }
                    }
                }
            }
            catch (Exception e)
            {

                throw e;
            }
            //Debug.Log($"Status received: {status.Operation}");
        }
        #endregion


        private void OnStatusSubscribeAnyTrading(List<string> channels)
        {
            foreach (var ch in channels)
            {
                Debug.Log($"{DebugTag} Subscribed ch={ch}");
            }
        }

        private void OnStatusUnsubscribeAnyTrading(List<string> channels)
        {
            foreach (var ch in channels)
            {
                Debug.Log($"{DebugTag} Unubscribed ch={ch}");
            }
        }

        private void OnMsgPayloadTradeInvite(TradeInvite invite)
        {
            ReceivedInvite?.Invoke(invite);
        }

        private void OnMsgPayloadOffer(OfferData offer)
        {
            ReceivedOffer?.Invoke(offer);
        }

        private void OnMsgPayloadInviteResponse(InviteResponseData response)
        {
            ReceivedInviteResponse?.Invoke(response);
        }

        private void OnMsgPayloadCommand(string str)
        {
            CommanReceived?.Invoke(str);
            Debug.Log($"{DebugTag} Command received: <{str}>");
        }

        private static string GetInbox(UserId userId)
        {
            return $"{TradingPreffix}.{userId}";
        }

        public void Dispose()
        {
            ReceivedInvite = null;
            ReceivedInviteResponse = null;
            ReceivedOffer = null;
            SessionPresenceChanged = null;
            ParticipantGoodbye = null;

            PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence -= OnPnPresence;
            PNManager.pubnubInstance.onPubNubObject -= OnPnObject;
            PNManager.pubnubInstance.onPubNubStatus -= OnPnStatus;
        }
    }
}