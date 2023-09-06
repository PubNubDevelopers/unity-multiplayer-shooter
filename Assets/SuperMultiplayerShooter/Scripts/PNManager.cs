using System.Collections.Generic;
using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;
using System;
using Newtonsoft.Json;
using Visyde;
using System.Linq;
using PubNubUnityShowcase;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

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

    //  Callback handler for PubNub messages and data
    private static SubscribeCallbackListener pnListener = null;

    //  Callbacks
    public delegate void PNMessageEvent(PNMessageResult<object> message);
    public PNMessageEvent onPubNubMessage;  
    public delegate void PNSignalEvent(PNSignalResult<object> message);
    public PNSignalEvent onPubNubSignal;
    public delegate void PNPresenceEvent(PNPresenceEventResult result);
    public PNPresenceEvent onPubNubPresence;  
    public delegate void PNObjectEvent(PNObjectEventResult result); // Receive app context (metadata) updates
    public PNObjectEvent onPubNubObject;
    public delegate void PNStatusEvent(PNStatus status); // Receive status updates
    public PNStatusEvent onPubNubStatus;
    public delegate void PubNubReady(); //  PubNub is initialized, ok to call PubNub object
    public PubNubReady onPubNubReady;

    //Initialize the static object, not for keeping the same instance of PubNub, but to retain the cached players and access
    //helper methods.
    private void Awake()
    {
        if (pubnubInstance != null)
        {
            Destroy(gameObject);
            return;
        }
        pubnubInstance = this;
        privateMessageUUID = "";
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Returns the PNConfiguration to reinitialize the PubNub object in different scenes.
    /// </summary>
    /// <returns></returns>
    public async Task InitializePubNub()
    {
        if (pubnubInstance.pubnub != null)
        {
            //  Not Initializing PubNub since it is not null
            onPubNubReady();
            return;
        }
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
        Initialize(userId);
        pnListener = new SubscribeCallbackListener();
        pubnub.AddListener(pnListener);
        pnListener.onMessage += OnPnMessage;
        pnListener.onSignal += OnPnSignal;
        pnListener.onObject += OnPnObject;
        pnListener.onPresence += OnPnPresence;
        pnListener.onStatus += OnPnStatus;
        await SubscribeToStaticChannels();
        try { onPubNubReady(); } catch (System.Exception) { }
    }

    private async Task SubscribeToStaticChannels()
    {
        pubnub.Subscribe<string>()
        .Channels(new List<string>() {
                        PubNubUtilities.chanChatLobby + "*",
                        PubNubUtilities.chanGlobal,
                        PubNubUtilities.chanGlobal + "-pnpres",   //  We only use presence events for the lobby channel
                        PubNubUtilities.chanPrefixLobbyRooms + "*",
                        PubNubUtilities.chanRoomStatus,
                        PubNubUtilities.chanChatAll,
                        PubNubUtilities.chanPrivateChat,
                        PubNubUtilities.chanChatTranslate + userId,
                        PubNubUtilities.chanLeaderboardSub,
                        PubNubUtilities.chanFriendRequest + userId
        })
        .ChannelGroups(new List<string>() {
                        PubNubUtilities.chanFriendChanGroupStatus + userId + "-pnpres", // Used for Monitoring online status of friends
                        PubNubUtilities.chanFriendChanGroupChat + userId
        })
        .Execute();
        await Task.Delay(100); //  Give some time for the subscription to finish
    }

    private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
    {
        if (result.Message != null)
        {
            //  Notify other listeners
            try
            {
                onPubNubMessage(result);
            }
            catch (System.Exception) { }
        }
    }

    private void OnPnSignal(Pubnub pn, PNSignalResult<object> result)
    {
        if (result.Message != null)
        {
            try
            {
                onPubNubSignal(result);
            }
            catch (System.Exception) { }
        }
    }

    private void OnPnPresence(Pubnub pn, PNPresenceEventResult result)
    {
        //  Notify other listeners
        try
        {
            onPubNubPresence(result);
        }
        catch (System.Exception) { }
    }

    //  Handler for PubNub Object event
    private void OnPnObject(Pubnub pn, PNObjectEventResult result)
    {
        //  Notify other listeners
        try
        {
            onPubNubObject(result);
        }
        catch (System.Exception) { }
    }

    // Handler for PubNub Status event.
    private void OnPnStatus(Pubnub pn, PNStatus status)
    {
        //  Notify other listeners
        try
        {
            onPubNubStatus(status);
        }
        catch (System.Exception) { }
    }

    protected virtual new void OnDestroy()
    {
        //  Don't want the default destroy behaviour
    }

    private void OnApplicationQuit()
    {
        pubnub?.UnsubscribeAll<string>();
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
    public async Task<string> GetUserNickname()
    {
        string nickname = "";
        if (pubnubInstance.CachedPlayers.ContainsKey(pubnub.GetCurrentUserId())
            && !string.IsNullOrWhiteSpace(PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name))
        {
            nickname = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
        }
        else
        {
            //Obtain the user metadata. IF the nickname cannot be found, set to be the first 6 characters of the UserId.
            await GetUserMetadata(pubnub.GetCurrentUserId());
            nickname = !string.IsNullOrWhiteSpace(PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name) ? PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name : pubnub.GetCurrentUserId();
        }

        return nickname;
    }

    /// <summary>
    /// Get the User Metadata given the UserId. Create and set user metadata when opening app for the
    /// first time.
    /// </summary>
    /// <param name="Uuid">UserId of the Player</param>
    public async Task<bool> GetUserMetadata(string Uuid)
    {
        //If they do not exist, pull in their metadata (since they would have already registered when first opening app), and add to cached players.                
        // Get Metadata for a specific UUID
        PNResult<PNGetUuidMetadataResult> getUuidMetadataResponse = await pubnub.GetUuidMetadata()
            .Uuid(Uuid)
            .IncludeCustom(true)
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

        //User has logged into the app for the first time. Set-up Metadata and register.
        else
        {
            // Setup metadata.
            Dictionary<string, object> customData = new Dictionary<string, object>();
            customData["hats"] = JsonConvert.SerializeObject(Connector.instance.GenerateRandomHats());
            customData["language"] = LocalizationSettings.SelectedLocale.Identifier.Code;
            customData["60fps"] = false;
            // Update
            await UpdateUserMetadata(pubnub.GetCurrentUserId(), pubnub.GetCurrentUserId(), customData);
        }

        return true;
    }

    /// <summary>
    /// Gets all of the user metadata. 
    /// Note: you'll need to ensure "Disallow Get All User Metadata" is unchecked for App Context in your PubNub Keys.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> GetAllUserMetadata()
    {
        PNResult<PNGetAllUuidMetadataResult> getAllUuidMetadataResponse = await pubnub.GetAllUuidMetadata()
            .IncludeCustom(true)
            .IncludeCount(true)
            .ExecuteAsync();

        PNGetAllUuidMetadataResult getAllUuidMetadataResult = getAllUuidMetadataResponse.Result;
        PNStatus status = getAllUuidMetadataResponse.Status;

        //Populate Cached Players Dictionary only if they have been set previously
        if (!status.Error && getAllUuidMetadataResult.TotalCount > 0)
        {
            foreach (PNUuidMetadataResult pnUUIDMetadataResult in getAllUuidMetadataResult.Uuids)
            {
                UserMetadata meta = new UserMetadata
                {
                    Uuid = pnUUIDMetadataResult.Uuid,
                    Name = pnUUIDMetadataResult.Name,
                    Email = pnUUIDMetadataResult.Email,
                    ExternalId = pnUUIDMetadataResult.ExternalId,
                    ProfileUrl = pnUUIDMetadataResult.ProfileUrl,
                    Custom = pnUUIDMetadataResult.Custom,
                    Updated = pnUUIDMetadataResult.Updated
                };

                if(!PNManager.pubnubInstance.CachedPlayers.ContainsKey(pnUUIDMetadataResult.Uuid))
                {
                    PNManager.pubnubInstance.CachedPlayers.Add(pnUUIDMetadataResult.Uuid, meta);
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update the User Metadata given the userid and name.
    /// </summary>
    /// <param name="Uuid">UserId of the player</param>
    /// <param name="name">The nickname of the player</param>
    /// <param name="metadata">The metadata to update</param>
    public async Task<bool> UpdateUserMetadata(string uuid, string name, Dictionary<string, object> metadata)
    {        
        PNResult<PNSetUuidMetadataResult> setUuidMetadataResponse = await pubnub.SetUuidMetadata()
            .Uuid(uuid)
            .Name(name)
            .Custom(metadata)
            .IncludeCustom(true)
            .ExecuteAsync();

        PNSetUuidMetadataResult setUuidMetadataResult = setUuidMetadataResponse.Result;
        PNStatus status = setUuidMetadataResponse.Status;

        //Update Cached Players.
        if (!status.Error && setUuidMetadataResult != null)
        {
            UserMetadata meta = new UserMetadata
            {
                Uuid = setUuidMetadataResult.Uuid,
                Name = setUuidMetadataResult.Name,
                Email = setUuidMetadataResult.Email,
                ExternalId = setUuidMetadataResult.ExternalId,
                ProfileUrl = setUuidMetadataResult.ProfileUrl,
                Custom = setUuidMetadataResult.Custom,
                Updated = setUuidMetadataResult.Updated
            };

            //Update hat inventory.
            if (setUuidMetadataResult.Custom != null && setUuidMetadataResult.Custom.ContainsKey("hats"))
            {
                List<int> availableHats = JsonConvert.DeserializeObject<List<int>>(setUuidMetadataResult.Custom["hats"].ToString());
                Connector.instance.UpdateAvailableHats(availableHats);
            }

            //Existing player
            if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(setUuidMetadataResult.Uuid))
            {
                PNManager.pubnubInstance.CachedPlayers[setUuidMetadataResult.Uuid] = meta;
            }

            // New Player
            else
            {
                PNManager.pubnubInstance.CachedPlayers.Add(setUuidMetadataResult.Uuid, meta);
            }
            return true;
        }

        else
        {
            Debug.Log($"Error setting Data ({PubNubUtilities.GetCurrentMethodName()}): {status.ErrorData.Information}");
        }

        return false;
    }

    /// <summary>
    /// Adds the channels to the channel group.
    /// </summary>
    /// <param name="channelGroup"></param>
    /// <param name="channels"></param>
    /// <returns></returns>
    public async Task<bool> AddChannelsToChannelGroup(string channelGroup, string[] channels)
    {
        PNResult<PNChannelGroupsAddChannelResult> cgAddChResponse = await pubnub.AddChannelsToChannelGroup()
            .ChannelGroup(channelGroup)
            .Channels(channels)
            .ExecuteAsync();
        if (!cgAddChResponse.Status.Error)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the channels from the channel group.
    /// </summary>
    /// <param name="channelGroup"></param>
    /// <param name="channels"></param>
    /// <returns></returns>
    public async Task<bool> RemoveChannelsFromChannelGroup(string channelGroup, string[] channels)
    {
        PNResult<PNChannelGroupsRemoveChannelResult> cgAddChResponse = await pubnub.RemoveChannelsFromChannelGroup()
            .ChannelGroup(channelGroup)
            .Channels(channels)
            .ExecuteAsync();
        if (!cgAddChResponse.Status.Error)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Testing purposes. Deletes the Channel Group.
    /// </summary>
    /// <param name="channelGroup"></param>
    /// <returns></returns>
    public async Task<bool> DeleteChannelGroup(string channelGroup)
    {
        PNResult<PNChannelGroupsDeleteGroupResult> delCgResponse = await pubnub.DeleteChannelGroup()
        .ChannelGroup("family")
        .ExecuteAsync();

        if(delCgResponse.Status != null && !delCgResponse.Status.Error)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Deletes all of the messages from the provided channel.
    /// Note: This requires three features to be fully functional:
    /// 1. Message Persistence is enabled for your key
    /// 2. There is a setting to accept delete from history requests for a key, which you must enable by checking the Enable Delete-From-History checkbox in the key settings for your key in the Admin Portal.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<bool> DeleteMessages(string channel)
    {
        PNResult<PNDeleteMessageResult> delMsgResponse = await pubnub.DeleteMessages()
            .Channel(channel)
            .ExecuteAsync();

        PNDeleteMessageResult delMsgResult = delMsgResponse.Result;
        PNStatus status = delMsgResponse.Status;

        if (status != null && status.Error)
        {
            //Check for any error
            Debug.Log(status.ErrorData.Information);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Store the UserId for when creating an option for the dropdown.
    /// Dropdowns will only store one dropdown option.
    /// </summary>
    public string PrivateMessageUUID
    {
        get { return privateMessageUUID; }
        set { privateMessageUUID = value; }
    }
}