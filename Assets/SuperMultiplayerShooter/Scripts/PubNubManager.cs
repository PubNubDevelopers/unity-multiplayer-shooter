using System.Collections;
using System.Collections.Generic;
using PubnubApi;
using PubnubApi.Unity;
using UnityEngine;

public class PubNubManager : MonoBehaviour
{
    //public static PubNubManager Instance;
    //Persist the PubNub object across scenes
    //private string _userID;

    

    //Cached players from connection.
   // private static Dictionary<string, PNUuidMetadataResult> _cachedPlayers = new Dictionary<string, PNUuidMetadataResult>();

   // public static Pubnub PubNub;

    /*

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
    public Pubnub InitializePubNub()
    {
        string userId = string.Empty;
        
        //Note: SystemInfo.deviceUniqueIdentifier does not work on WebGL Builds.
        //Check if the user id already exists on this device. If not, save it.
        if (PlayerPrefs.HasKey("uuid"))
        {
            userId = PlayerPrefs.GetString("uuid");
        }

        else
        {
            userId = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetString("uuid", userId);
        }

        PNConfiguration pnConfiguration = new PNConfiguration(new UserId(userId))
        {
            SubscribeKey = "SUBSCRIBE_KEY",
            PublishKey = "PUBLISH_KEY",
            LogVerbosity = PNLogVerbosity.BODY
        };

        return new Pubnub(pnConfiguration);
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
    public Dictionary<string, PNUuidMetadataResult> CachedPlayers
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
    */
}