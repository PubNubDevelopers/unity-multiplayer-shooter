using System.Collections;
using System.Collections.Generic;
using PubNubAPI;
using UnityEngine;

public class PubNubManager : MonoBehaviour
{
    public static PubNubManager PubNubConnection;
    //Persist the PubNub object across scenes
    private static string _userID;

    //Cached players from connection.
    private static Dictionary<string, PNUUIDMetadataResult> _cachedPlayers = new Dictionary<string, PNUUIDMetadataResult>();

    public static PubNub PubNub;

    public static string PublishKey
    {
        get { return "test publish key"; }
    }

    public static string SubscribeKey
    {
        get { return "test subscribe key"; }
    }

    public static string UserID
    {
        get { return _userID; }
        set { _userID = value; }
    }
    private void Awake()
    {
        if (PubNubConnection != null)
        {
            Destroy(gameObject);
            return;
        }
        PubNubConnection = this;
        DontDestroyOnLoad(gameObject);
    }

    public static Dictionary<string, PNUUIDMetadataResult> CachedPlayers
    {
        get { return _cachedPlayers; }
        set { _cachedPlayers = value; }
    }

    public static void PublishMessage(string message, string channel, Dictionary<string,string> meta)
    {
        PubNub.Publish()
               .Channel(channel)
               .Message(message)
                .Meta(meta)
               .Async((result, status) => {
                   if (!status.Error)
                   {
                       Debug.Log(string.Format("Result: {0}", result.Timetoken));
                   }
                   else
                   {
                       Debug.Log(status.Error);
                       Debug.Log(status.ErrorData.Info);
                   }
               });
    }
}