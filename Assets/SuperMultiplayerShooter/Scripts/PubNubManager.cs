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
}