using Newtonsoft.Json;

namespace PubNubUnityShowcase
{
    public struct TradeInvite : IJsonSerializable
    {
        public TradeInvite(TradeSessionData sessionData, OfferData offerData)
        {
            SessionData = sessionData;
            OfferData = offerData;
        }

        [JsonProperty("session")]
        public TradeSessionData SessionData { get; set; }

        [JsonProperty("offer")]
        public OfferData OfferData { get; set; }

        /// <summary>
        /// Channel to send invite resposnes
        /// </summary>
        [JsonIgnore]       
        public string RSVPChannel => $"{PubNubTrading.TradingPreffix}.{SessionData.Initiator.UserID}";
    }
}