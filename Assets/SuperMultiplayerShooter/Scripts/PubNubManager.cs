using System.Collections;
using System.Collections.Generic;
using PubNubAPI;
using UnityEngine;

public class PubNubManager : MonoBehaviour
{
    public static PubNubManager Instance;
    //Persist the PubNub object across scenes
    private string _userID;

    //Cached players from connection.
    private static Dictionary<string, PNUUIDMetadataResult> _cachedPlayers = new Dictionary<string, PNUUIDMetadataResult>();

    public static PubNub PubNub;

    /// <summary>
    /// Do not destroy the PubNubManager object when transitioning between scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns the PNConfiguration to reinitialize the PubNub object in different scenes.
    /// </summary>
    /// <returns></returns>
    public PubNub InitializePubNub()
    {    
        PNConfiguration pnConfiguration = new PNConfiguration();
        pnConfiguration.SubscribeKey = "SUBSCRIBE_KEY";
        pnConfiguration.PublishKey = "PUBLISH_KEY";
        pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;

        //Randomly generates a username. SystemInfo.deviceUniqueIdentifier does not work on WebGL Builds.
        string uuid = "User#" + Random.Range(0, 9999).ToString();
        //Check if the user id already exists on this device. If not, save it.
        if (PlayerPrefs.HasKey("uuid"))
        {
            uuid = PlayerPrefs.GetString("uuid");
        }

        else
        {
            PlayerPrefs.SetString("uuid", uuid);
        }

        _userID = uuid;

        pnConfiguration.UserId = uuid;
        return new PubNub(pnConfiguration);
    }

    /// <summary>
    /// The UserId of the PubNub Connection.
    /// </summary>
    public string UserId
    {
        get { return _userID; }
        set { _userID = value; }
    }

    /// <summary>
    /// Tracks a cached list of all players to be used throughout the application.
    /// </summary>
    public Dictionary<string, PNUUIDMetadataResult> CachedPlayers
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