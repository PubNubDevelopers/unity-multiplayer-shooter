using Newtonsoft.Json;
using PubnubApi;
using UnityEngine;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct TradeSessionData : IJsonSerializable
    {
        [SerializeField] private long sessionID;
        [SerializeField] private TraderData initiator;
        [SerializeField] private TraderData responder;

        public TradeSessionData(long sessionID, TraderData initiator, TraderData responder)
        {
            this.sessionID = sessionID;
            this.initiator = initiator;
            this.responder = responder;
        }

        public long Id { get => sessionID; set => sessionID = value; }
        public TraderData Initiator { get => initiator; set => initiator = value; }
        public TraderData Respondent { get => responder; set => responder = value; }

        [JsonIgnore]
        public string Channel => $"trading.sessions.{sessionID}";

        public TraderData GetParticipantById(string id)
        {
            if(initiator.UserID == id)
                return initiator;

            if (responder.UserID == id)
                return responder;

            Debug.LogWarning($"MissingParticipant: sessionId={sessionID}");
            return default;
        }

        public UserId GetUser(Role role)
        {
            switch (role)
            {
                case Role.Initiator:
                    return initiator.UserID;
                case Role.Respondent:
                    return initiator.UserID;
                default:
                    return "ERR";
            }
        }

        public enum Role
        {
            Initiator = 0,
            Respondent = 1
        }
    }


    //public enum UnexpectedLeave
    //{
    //    join = 0,
    //    busyGameOrLobby,
    //    busyTrading
    //}
}
