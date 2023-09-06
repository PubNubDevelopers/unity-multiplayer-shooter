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
        private readonly Pubnub _pnApi;

        public PubNubTrading()
        {
            _pnApi = Connector.instance.GetPubNubObject();
            Connector.instance.onPubNubMessage += OnPnMessage;
            Connector.instance.onPubNubPresence += OnPnPresence;
            Connector.instance.onPubNubObject += OnPNObject;
            Connector.instance.PNStatusReceived += OnPnStatus;
        }

        private Pubnub PNApi { get => Connector.instance.GetPubNubObject(); }
        private UserId ThisUser => _pnApi.GetCurrentUserId();
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

            await PNApi.Publish()
                .Message(invite)
                .Channel(respInbox)
                .Meta(MessageNormalilzation.GetMeta<TradeInvite>())
                .ShouldStore(true)
                .ExecuteAsync();

            //Debug.Log($"{DebugTag} SendInvite ch={invite.RSVPChannel}");
        }

        public async Task SendInviteResponse(TradeInvite invite, InviteResponseData response)
        {
            await PNApi.Publish()
                .Message(response)
                .Channel(invite.RSVPChannel)
                .Meta(MessageNormalilzation.GetMeta<InviteResponseData>())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task<OfferData> SendOffer(TradeSessionData session, OfferData offer)
        {
            await PNApi.Publish()
                .Message(offer)
                .Channel(session.Channel)
                .Meta(MessageNormalilzation.GetMeta<OfferData>())
                .ShouldStore(true)
                .ExecuteAsync();

            return offer;
        }

        public async Task SendLeaveMessage(TradeSessionData session, LeaveSessionData leaveData)
        {
            await PNApi.Publish()
                .Message(leaveData)
                .Channel(session.Channel)
                .Meta(MessageNormalilzation.GetMeta<LeaveSessionData>())
                .ShouldStore(true)
                .ExecuteAsync();
        }

        public async Task SendCommand(TradeSessionData session, string cmd)
        {
            await PNApi.Publish()
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
                PNApi.Subscribe<string>()
                    .Channels(new[] { GetInbox(ThisUser) })
                    .Execute();

                await Task.Delay(2000);
                //Debug.Log($"{DebugTag} Subscribed ch={GetInbox(ThisUser)}");

                return GetInbox(ThisUser);
            }
            catch (Exception e) { throw e; }
        }

        public async Task UnubscribeToTradeInvites()
        {
            try
            {
                PNApi.Unsubscribe<string>()
                    .Channels(new[] { GetInbox(ThisUser) })
                    .Execute();

                await Task.Delay(2000);
                //Debug.Log($"{DebugTag} Unsubscribed ch={GetInbox(ThisUser)}");
            }
            catch (Exception e) { throw e; }
        }

        public async Task SubscribeSession(TradeSessionData session)
        {
            PNApi.Subscribe<string>()
                .Channels(new[] { session.Channel })
                .WithPresence()
                .Execute();

            await Task.Delay(2000);
        }

        public async Task UnsubscribeSession(TradeSessionData session)
        {
            PNApi.Unsubscribe<string>()
                .Channels(new[] { session.Channel, $"{session.Channel}-pnpres" })
                .Execute();

            await Task.Delay(2000);
            Debug.Log($"<color=red>[Network]</color> Session({session.Id}): unsubscribe ch={session.Channel}-pnpres");
        }

        public async Task<TradeInventoryData> GetTraderInventory(UserId user)
        {
            //TODO: handle unsuccessfull requests
            try
            {
                var response = await PNApi.GetUuidMetadata()
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

                var initiatorSuccess = await PNApi.SetUuidMetadata()
                    .Uuid(session.Initiator.UserID)
                    .Name(initiatorCurrent.Name)
                    .Custom(initiatorCurrent.Custom)
                    .ExecuteAsync();

                var respondentSuccess = await PNApi.SetUuidMetadata()
                    .Uuid(session.Respondent.UserID)
                    .Name(respondentCurrent.Name)
                    .Custom(respondentCurrent.Custom)
                    .ExecuteAsync();
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
            //Debug.Log($"{DebugTag} MSG: json=>>>{result.Message}<<<");

            if (result.UserMetadata != null && result.UserMetadata is Dictionary<string, object>)
            {
                var meta = (Dictionary<string, object>)result.UserMetadata;
                string typeKey = meta[MessageNormalilzation.TYPE_KEY] as string;

                //Debug.Log($"{DebugTag} MSG<{typeKey}> ch={result.Channel} | paylaod= {json} ");

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

        private void OnPNObject(PNObjectEventResult result)
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

            Connector.instance.onPubNubMessage -= OnPnMessage;
            Connector.instance.onPubNubPresence -= OnPnPresence;
        }
    }
}