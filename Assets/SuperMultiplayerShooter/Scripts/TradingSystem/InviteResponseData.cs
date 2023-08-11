using PubNubUnityShowcase;

public struct InviteResponseData : IJsonSerializable
{
    public InviteResponseData(bool willJoin, bool inTradingSession, bool inGameOrLobby)
    {
        WillJoin = willJoin;
        InGameOrLobby = inGameOrLobby;
        InTradingSession = inTradingSession;
    }

    public bool WillJoin { get; set; }
    public bool InGameOrLobby { get; set; }
    public bool InTradingSession { get; set; }
}
