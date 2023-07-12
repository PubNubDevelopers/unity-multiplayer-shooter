using System.Collections.Generic;
using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;

public class PNManager : PNManagerBehaviour
{
    // UserId identifies this client.
    public string userId;

    //Persist the PubNub object across scenes
    public static PNManager pubnubInstance;

    //Cached players from connection.
    private static Dictionary<string, UserMetadata> cachedPlayers = new Dictionary<string, UserMetadata>();

    private async void Awake()
    {
        if (pubnubInstance == null)
        {
            // Initialize will create a PubNub instance, pass the configuration object, and prepare the listener. 
            InitializePubNub();
            pubnubInstance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Returns the PNConfiguration to reinitialize the PubNub object in different scenes.
    /// </summary>
    /// <returns></returns>
    private Pubnub InitializePubNub()
    {
        if (string.IsNullOrWhiteSpace(pnConfiguration.PublishKey) || string.IsNullOrWhiteSpace(pnConfiguration.SubscribeKey))
        {
            Debug.LogError("Please set your PubNub keys in the PNConfigAsset.");
        }
        //Randomly generates a username. SystemInfo.deviceUniqueIdentifier does not work on WebGL Builds.
        //string uuid = "User#" + Random.Range(0, 9999).ToString();
        string uuid = System.Guid.NewGuid().ToString();

        //Check if the user id already exists on this device. If not, save it.
        if (PlayerPrefs.HasKey("uuid"))
        {
            uuid = PlayerPrefs.GetString("uuid");
        }

        else
        {
            PlayerPrefs.SetString("uuid", uuid);
        }

        userId = uuid;

        return Initialize(userId);
    }


    /// Tracks a cached list of all players to be used throughout the application.
    /// </summary>
    public Dictionary<string, UserMetadata> CachedPlayers
    {
        get { return cachedPlayers; }
        set { cachedPlayers = value; }
    }
}