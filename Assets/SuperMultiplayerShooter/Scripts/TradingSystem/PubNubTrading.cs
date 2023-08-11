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
        }

        private Pubnub PNApi { get => _pnApi; }
        private UserId ThisUser => _pnApi.GetCurrentUserId();
        private Dictionary<string, UserMetadata> DatastoreUserMetadata => PNManager.pubnubInstance.CachedPlayers;
        private string DebugTag => $"<color=red>[Network]</color>";

        public event Action<OfferData> ReceivedOffer;
        public event Action<TradeInvite> ReceivedInvite;
        public event Action<InviteResponseData> ReceivedInviteResponse;
        public event Action<LeaveSessionData> ParticipantGoodbye;   //If least with explicid SendLeaveMessage()
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
            Debug.Log($"{DebugTag} SendInvResponse ch={invite.RSVPChannel}");
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

        public async Task<string> SubscribeToTradeInvites()
        {
            PNApi.Subscribe<string>()
                .Channels(new[] { GetInbox(ThisUser) })
                .Execute();

            await Task.Delay(2000);
            //Debug.Log($"{DebugTag} Subscribed ch={GetInbox(ThisUser)}");

            return GetInbox(ThisUser);
        }

        public async Task UnubscribeToTradeInvites()
        {
            PNApi.Unsubscribe<string>()
                .Channels(new[] { GetInbox(ThisUser) })
                .Execute();

            await Task.Delay(2000);
            Debug.Log($"{DebugTag} Unsubscribed ch={GetInbox(ThisUser)}");
        }

        public async Task SubscribeSession(TradeSessionData session)
        {
            PNApi.Subscribe<string>()
                .Channels(new[] { session.Channel })
                .WithPresence()
                .Execute();

            await Task.Delay(2000);
            //Debug.Log($"{DebugTag} Subscribed ch={session.Channel}");
        }

        public async Task UnsubscribeSession(TradeSessionData session)
        {
            PNApi.Unsubscribe<string>()
                .Channels(new[] { session.Channel, $"{session.Channel}-pnpres" })
                .Execute();

            await Task.Delay(2000);
            //Debug.Log($"<color=red>[Network]</color> Session({session.Id}): unsubscribe ch={session.Channel}-pnpres");
        }

        /// <param name="fallback">Won't be needed if it can be taken fom cache</param>
        public async Task<TraderData> GetTraderData(UserId user, TraderData fallback)
        {
            try
            {
                var response = await PNApi.GetUuidMetadata()
                    .Uuid(user)
                    .IncludeCustom(true)
                    .ExecuteAsync();

                TraderData traderData = new TraderData(
                    response.Result.Uuid,
                    response.Result.Name,
                    fallback.PlayerAvatarType,
                    new TradeInventoryData(MetadataNormalization.GetHats(response.Result.Custom)),
                    fallback.EquippedCosmetic);

                return traderData;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task ApplyMetadata(TradeSessionData session, OfferData offer)
        {
            try
            {
                DatastoreUserMetadata.TryGetValue(session.Initiator.UserID, out var initiatorCurrent);
                DatastoreUserMetadata.TryGetValue(session.Respondent.UserID, out var respondentCurrent);

                MetadataNormalization.ReplaceHats(initiatorCurrent.Custom, offer.InitiatorGives, offer.InitiatorReceives);
                MetadataNormalization.ReplaceHats(respondentCurrent.Custom, offer.InitiatorReceives, offer.InitiatorGives);

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
            else
                Debug.LogWarning($"Received unknown payload type.");
        }

        private void OnPnPresence(PNPresenceEventResult result)
        {
            if (!result.Channel.StartsWith(TradingPreffix))
                return;

            //it is safe to assume that only the current session channel will receive presence events so no need to check the channel
            SessionPresenceChanged?.Invoke(result.Uuid, result.Event);

            //string json = JsonConvert.SerializeObject(result);
            //Debug.Log($"<color=red>[Network]</color> Received Presence event:{result.Event} uuid={result.Uuid}");            
        }
        #endregion

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
            throw new NotImplementedException();
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

            //Debug.Log($"{DebugTag} Disposed");
        }
    }
}