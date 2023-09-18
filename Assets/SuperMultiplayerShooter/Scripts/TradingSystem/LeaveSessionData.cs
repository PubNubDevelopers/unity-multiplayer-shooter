namespace PubNubUnityShowcase
{
    public struct LeaveSessionData : IJsonSerializable
    {
        public LeaveSessionData(TraderData participant, LeaveReason reason)
        {
            Participant = participant;
            Reason = reason;
        }

        public TraderData Participant { get; set; }
        public LeaveReason Reason { get; set; }
    }

    public enum LeaveReason
    {
        withdrawOffer = 1,
        transactionComplete = 2,
        otherPartyBusy = 3,
        otherPartyNotResponding = 4
    }
}