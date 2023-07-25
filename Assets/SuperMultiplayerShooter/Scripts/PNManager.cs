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

    //The list of private message connections a user can quickly connect to.
    private static string privateMessageUUID = "";

    //Initialize the static object, not for keeping the same instance of PubNub, but to retain the cached players and access
    //helper methods.
    private void Awake()
    {
        pubnubInstance = this;
        DontDestroyOnLoad(gameObject);
    }


    /// <summary>
    /// Returns the PNConfiguration to reinitialize the PubNub object in different scenes.
    /// </summary>
    /// <returns></returns>
    public Pubnub InitializePubNub()
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

    /// <summary>
    /// Returns the user's nickname. If it is not cached, it will obtain this information.
    /// </summary>
    /// <returns></returns>
    public string GetUserNickname()
    {
        string nickname = "";
        if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(pubnub.GetCurrentUserId())
            && !string.IsNullOrWhiteSpace(PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name))
        {
            nickname = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
        }

        else
        {
            //Obtain the user metadata. IF the nickname cannot be found, set to be the first 6 characters of the UserId.
            GetUserMetadata(pubnub.GetCurrentUserId());
            nickname = !string.IsNullOrWhiteSpace(PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name) ? PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name : pubnub.GetCurrentUserId();
        }

        return nickname;
    }

    /// <summary>
    /// Get the User Metadata given the UserId.
    /// </summary>
    /// <param name="Uuid">UserId of the Player</param>
    public async void GetUserMetadata(string Uuid)
    {
        //If they do not exist, pull in their metadata (since they would have already registered when first opening app), and add to cached players.                
        // Get Metadata for a specific UUID
        PNResult<PNGetUuidMetadataResult> getUuidMetadataResponse = await pubnub.GetUuidMetadata()
            .Uuid(Uuid)
            .ExecuteAsync();
        PNGetUuidMetadataResult getUuidMetadataResult = getUuidMetadataResponse.Result;
        PNStatus status = getUuidMetadataResponse.Status;
        if (!status.Error && getUuidMetadataResult != null)
        {
            UserMetadata meta = new UserMetadata
            {
                Uuid = getUuidMetadataResult.Uuid,
                Name = getUuidMetadataResult.Name,
                Email = getUuidMetadataResult.Email,
                ExternalId = getUuidMetadataResult.ExternalId,
                ProfileUrl = getUuidMetadataResult.ProfileUrl,
                Custom = getUuidMetadataResult.Custom,
                Updated = getUuidMetadataResult.Updated
            };
            if (!PNManager.pubnubInstance.CachedPlayers.ContainsKey(getUuidMetadataResult.Uuid))
            {
                PNManager.pubnubInstance.CachedPlayers.Add(getUuidMetadataResult.Uuid, meta);
            }
        }
    }

    public string PrivateMessageUUID
    {
        get { return privateMessageUUID; }
        set { privateMessageUUID = value; }
    }
}