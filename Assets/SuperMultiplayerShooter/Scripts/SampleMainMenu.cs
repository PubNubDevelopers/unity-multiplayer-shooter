using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using PubnubApi;
using PubnubApi.Unity;
using Newtonsoft.Json;
using System;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine.Networking;

namespace Visyde
{
    /// <summary>
    /// Sample Main Menu
    /// - A sample script that handles the main menu UI.
    /// </summary>

    public class SampleMainMenu : MonoBehaviour
    {
        [Header("Main:")]
        public Text connectionStatusText;
        public Button findMatchBTN;
        public Button customMatchBTN;
        public GameObject findMatchCancelButtonObj;
        public GameObject findingMatchPanel;
        public GameObject customGameRoomPanel;
        public Text matchmakingPlayerCountText;
        public InputField playerNameInput;
        public GameObject messagePopupObj;
        public Text messagePopupText;
        public GameObject characterSelectionPanel;
        public Image characterIconPresenter;
        public GameObject loadingPanel;
        public Toggle frameRateSetting;
        public Text totalCountPlayers;
        public Button searchPlayers;
        public InputField searchPlayersInput;
        public GameObject playerListArea;
        public Transform searchPlayerContent;
        public Transform searchPlayer;
        public Image onlineStatus;

        // leaderboard
        public Text leaderboardText;
        public Text namePos1;
        public Text namePos2;
        public Text namePos3;
        public Text namePos4;
        public Text namePos5;

        public Text kdPos1;
        public Text kdPos2;
        public Text kdPos3;
        public Text kdPos4;
        public Text kdPos5;
        
        private string _leaderboardChannelPub = "score.leaderboard";
        private string _leaderboardChannelSub = "leaderboard_scores";

        //Friend List
        public Button friendListBtn;
        public Text totalFriendCountText; //total number of friends user has, both online/offline.
        public Text onlineFriendsCountText; //number of friends that are currently online.
        public GameObject friendListArea;
        public Transform playerContainer;
        public Transform player;
        private List<Transform> friendList = new List<Transform>();
        private List<Transform> filteredPlayers = new List<Transform>();

        //Helper properties
        private string _privateChannel = "presence-";
        private string _publicChannel = "chat.public"; // Used for global presence, all chat, etc.
        private string _cgFriendList = "friends-"; //Used to track presence events for friends. Used to determine when friends come online/offline.
        private Pubnub pubnub;
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Initializes the PubNub Connection.
            pubnub = PNManager.pubnubInstance;

            //Add Listeners
            pubnub.AddListener(listener);
            listener.onMessage += OnPnMessage;
            listener.onPresence += OnPnPresence;
            listener.onObject += OnPnObject;

            _privateChannel += pubnub.GetCurrentUserId(); //Private channels in form of "presence-<UserId>". Necessary for buddy list tracking.
            _cgFriendList += pubnub.GetCurrentUserId(); //Manages the friend lists.
         
            //Obtain and cache user metadata.
            GetAllUserMetadata();

            //Add client to Channel Group just in case first time load. Will not add if already present.
            AddChannelsToGroup(_cgFriendList, new string[] { _privateChannel });
           
            //Subscribe to the list of Channels
            //Currently listening on a public channel (for presence and all chat for all users)
            //and a buddylist channel for personal user to listen for any buddy list events.
            pubnub.Subscribe<string>()
               .Channels(new string[]
               {
                   _publicChannel,
                   _privateChannel,
                   _leaderboardChannelSub
               })
               .ChannelGroups(new string[]
               {
                   _cgFriendList + "-pnpres" // Watch friends come online/go offline by watching presence events of this channel group and not message of users.
               })
               .WithPresence()
               .Execute();

            //Loads the Initial Online Occupants and Populate Friend List.
            InitialPresenceFriendListLoad();

            //fire a refresh command to the pubnub function to get the leaderboard to update
            PublishMessage("{\"username\":\"\",\"score\":\"\",\"refresh\":\"true\"}", _leaderboardChannelPub);

            // Others:
            frameRateSetting.isOn = Application.targetFrameRate == 60;
        }

        /// <summary>
        /// Publishes a Message to the PubNub Network
        /// </summary>
        /// <param name="text"></param>
        private async void PublishMessage(string text, string channel)
        {
            await pubnub.Publish()
             .Channel(channel)
             .Message(text)
             .ExecuteAsync();
        }

        /// <summary>
        /// Event listener to handle PubNub Message events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            // Debug.Log($"Message received: {result.Message}");

            // Leaderboard Updates
            if (result.Channel.Equals(_leaderboardChannelSub))
            {
                Debug.Log(result.Message);
                Dictionary<string, object> msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());// as Dictionary<string, object>;
                //Dictionary<string, object> msg = result.Message
                var usernames = (msg["username"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();
                var scores = (msg["score"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();

                if (usernames[0] != null)
                {
                    namePos1.text = usernames[0];
                    kdPos1.text = scores[0];
                }

                if (usernames[1] != null)
                {
                    namePos2.text = usernames[1];
                    kdPos2.text = scores[1];
                }

                if (usernames[2] != null)
                {
                    namePos3.text = usernames[2];
                    kdPos3.text = scores[2];
                }

                if (usernames[3] != null)
                {
                    namePos4.text = usernames[3];
                    kdPos4.text = scores[3];
                }

                if (usernames[4] != null)
                {
                    namePos5.text = usernames[4];
                    kdPos5.text = scores[4];
                }
            }
        }

        /// <summary>
        /// Event listener to handle PubNub Presence events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnPresence(Pubnub pn, PNPresenceEventResult result)
        {
            // Debug.Log(result.Event);
            if (result.Channel.Equals(_publicChannel))
            {
                totalCountPlayers.text = result.Occupancy.ToString();

                //When user joins, check their UUID in cached players to determine if they are a new player.                 
                if (!PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.Uuid))
                {
                    GetUserMetadata(result.Uuid);
                }
            }

            //Friend List - Detect current friend online status. Ignore self.
            else if (result.Subscription.Equals(_cgFriendList) && !pubnub.GetCurrentUserId().Equals(result.Uuid))
            {
                //Friend is offline when they leave or timeout.
                bool isOnline = !(result.Event.Equals("leave") || result.Event.Equals("timeout"));
                //Search for the friend and update status
                UpdateCachedPlayerOnlineStatus(result.Uuid, isOnline);
            }
        }

        /// <summary>
        /// Event listener to handle PubNub Object events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnObject(Pubnub pn, PNObjectEventResult result)
        {
            //Catch other player updates (when their name changes, etc)
            if (result != null)
            {
                //User Metadata Update from another player, such as a name change. Update chached players, as well as friend list.
                if (result.Type.Equals("uuid"))
                {
                    UserMetadata meta = new UserMetadata
                    {
                        Uuid = result.UuidMetadata.Uuid,
                        Name = result.UuidMetadata.Name,
                        Email = result.UuidMetadata.Email,
                        ExternalId = result.UuidMetadata.ExternalId,
                        ProfileUrl = result.UuidMetadata.ProfileUrl,
                        Custom = result.UuidMetadata.Custom,
                        Updated = result.UuidMetadata.Updated
                    };
                    PNManager.pubnubInstance.CachedPlayers[result.UuidMetadata.Uuid] = meta;

                    //Update friend list if the update user has updates to make.
                    //No need to update player search, as it pulls from cached players.
                    Transform updateFriend = friendList.Find(player => player.name.Equals(result.UuidMetadata.Uuid));
                    if (updateFriend != null)
                    {
                        updateFriend.Find("PlayerUsername").GetComponent<Text>().text = result.UuidMetadata.Name;
                    }
                }
                //Friend List - Triggerred whenever channel membership is updated (client gets added/removed from another player's friend list)
                else if (result.Type.Equals("membership"))
                {
                    //Another player has added client as a friend
                    if (result.Event.Equals("set"))
                    {
                        AddFriend(result.UuidMetadata.Uuid);
                    }

                    //Another player has removed client as a friend.
                    else if (result.Event.Equals("delete"))
                    {
                        RemoveFriend(result.UuidMetadata.Uuid);
                    }
                }
            }
        }
        
        // Update is called once per frame
        void Update()
        {
            bool connecting = !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.NetworkClientState == ClientState.ConnectedToNameServer || PhotonNetwork.InRoom;

            // Handling texts:
            connectionStatusText.text = connecting ? PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer ? "Connecting..." : "Finding network..."
                : "Connected! (" + PhotonNetwork.CloudRegion + ") | Ping: " + PhotonNetwork.GetPing();
            connectionStatusText.color = PhotonNetwork.IsConnectedAndReady ? Color.green : Color.yellow;
            matchmakingPlayerCountText.text = PhotonNetwork.InRoom ? Connector.instance.totalPlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers : "Matchmaking...";

            // Handling buttons:
            customMatchBTN.interactable = !connecting;
            findMatchBTN.interactable = !connecting;
            findMatchCancelButtonObj.SetActive(PhotonNetwork.InRoom);

            // Handling panels:
            customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);
            loadingPanel.SetActive(PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer || PhotonNetwork.NetworkClientState == ClientState.DisconnectingFromGameServer);

            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }
        }

        /// <summary>
        /// Called whenever the scene or game ends. Unsubscribe event listeners.
        /// </summary>
        void OnDestroy()
        {
            listener.onMessage -= OnPnMessage;
            listener.onPresence -= OnPnPresence;
            listener.onObject -= OnPnObject;

            //Clear out the cached players when changing scenes. The list needs to be updated when returning to the scene in case
            //there are new players.
            PNManager.pubnubInstance.CachedPlayers.Clear();
        }

        /// <summary>
        /// Get the User Metadata given the UserId.
        /// </summary>
        /// <param name="Uuid">UserId of the Player</param>
        private async void GetUserMetadata(string Uuid)
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

        /// <summary>
        /// Obtains all player metadata from this PubNub Keyset to cache.
        /// </summary>
        private async void GetAllUserMetadata()
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

                    PNManager.pubnubInstance.CachedPlayers.Add(pnUUIDMetadataResult.Uuid, meta);
                }
            }

            //Change playerInput name to be set to username of the user as long as the name was originally set.
            if (PNManager.pubnubInstance.CachedPlayers.Count > 0 && PNManager.pubnubInstance.CachedPlayers.ContainsKey(pubnub.GetCurrentUserId()))
            {
                playerNameInput.text = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
                //Nickname is used throughout the system to define the player
                //TODO: Remove once Photon Engine is removed for finding and supporting multiplayer sync.
                PhotonNetwork.NickName = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
            }
            //If current user cannot be found in cached players, then a new user is logged in. Set the metadata and add.
            else
            {
                // Set Metadata for UUID set in the pubnub instance
                PNResult<PNSetUuidMetadataResult> setUuidMetadataResponse = await pubnub.SetUuidMetadata()
                    .Uuid(pubnub.GetCurrentUserId())
                    .Name(pubnub.GetCurrentUserId())
                    .ExecuteAsync();
                PNSetUuidMetadataResult setUuidMetadataResult = setUuidMetadataResponse.Result;
                PNStatus setUUIDResponseStatus = setUuidMetadataResponse.Status;

                if (!setUUIDResponseStatus.Error)
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
                    PNManager.pubnubInstance.CachedPlayers.Add(setUuidMetadataResult.Uuid, meta);
                    playerNameInput.text = setUuidMetadataResult.Uuid;
                }

                else
                {
                    Debug.Log($"An error has occurred: {setUUIDResponseStatus.Error}");
                }
            }
        }

        /// <summary>
        /// Initial presence request for the channel name. Used for the initial total count, as well as to
        /// construct the client's friend list.
        /// </summary>
        private async void InitialPresenceFriendListLoad()
        {
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
                .Channels(new string[] {
                    _publicChannel
                })
                .IncludeState(true)
                .IncludeUUIDs(true)
                .ExecuteAsync();

            PNHereNowResult herenowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;

            if (status.Error)
            {
                UnityEngine.Debug.Log(string.Format("HereNow Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
            }
            //Success
            else
            {
                //Initial count of players in the game
                if (herenowResult.Channels.Count > 0)
                {
                    totalCountPlayers.text = herenowResult.Channels[_publicChannel].Occupants.Count.ToString();

                    //Use this list to construct the friend list for the client.
                    GetFriendList(herenowResult.Channels[_publicChannel].Occupants);
                }
            }
        }

        /// <summary>
        /// Load the Client's Friend List via the channels associated with the client.
        /// </summary>
        private async void GetFriendList(List<PNHereNowOccupantData> onlinePlayers)
        {
            int onlineFriendCount = 0;
            int totalFriendCount = 0;

            PNResult<PNMembershipsResult> getMembershipsResponse = await pubnub.GetMemberships()
                .Uuid(pubnub.GetCurrentUserId())
                .Include(new PNMembershipField[] { PNMembershipField.CUSTOM, PNMembershipField.CHANNEL, PNMembershipField.CHANNEL_CUSTOM })
                .IncludeCount(true)
                .Page(new PNPageObject() { Next = "", Prev = "" })
                .ExecuteAsync();

            PNMembershipsResult getMembeshipsResult = getMembershipsResponse.Result;
            PNStatus status = getMembershipsResponse.Status;

            //Cross-reference the onlinePlayers list with that of the channel members
            if (!status.Error && getMembeshipsResult.TotalCount > 0)
            {
                //The players that are online and match those in the client's members for their channel (space)
                //will be placed in the online friends category.
                for (int i = 0; i < getMembeshipsResult.TotalCount; i++)// (PNMembers members in result.Data)
                {
                    //Only looking for users apart of friend list.
                    if (getMembeshipsResult.Memberships[i].ChannelMetadata.Channel.StartsWith("presence"))
                    {
                        //extract user from channel name (remove the presence-)
                        string userId = getMembeshipsResult.Memberships[i].ChannelMetadata.Channel.Substring(9);

                        //Do not include self.
                        if (!pubnub.GetCurrentUserId().Equals(userId))
                        {
                            Color onlineStatus = Color.gray;
                            //The online status changes to green if there is a match in the onlinePlayers initial call.
                            if (onlinePlayers.Count > 0 && onlinePlayers.Find(onlineUser => onlineUser.Uuid.Equals(userId)) != null)
                            {
                                onlineStatus = Color.green;
                                onlineFriendCount++;
                            }

                            //Create player transforms for the buddy list. Can be retained and not consistenly destroyed like filtered friends.
                            Transform playerTrans = Instantiate(player, playerContainer);
                            //Use the UserId in case there is no name associated with the friend.
                            playerTrans.Find("PlayerUsername").GetComponent<Text>().text = !string.IsNullOrWhiteSpace(PNManager.pubnubInstance.CachedPlayers[userId].Name) ? PNManager.pubnubInstance.CachedPlayers[userId].Name : userId;
                            playerTrans.gameObject.name = userId; // Used to find the object later on.
                            playerTrans.Find("OnlineStatus").GetComponent<Image>().color = onlineStatus;
                            playerTrans.gameObject.SetActive(true);
                            friendList.Add(playerTrans);
                            totalFriendCount++;
                        }
                    }
                }
            }

            //The client has not registered themselves for their buddy list before. Register the client as a member of their buddy list to receive presence and the removal/addition of other users..
            else
            {
                PNMembership membership = new PNMembership()
                {
                    Channel = _privateChannel
                };

                PNResult<PNMembershipsResult> setMembershipsResponse = await pubnub.SetMemberships()
                    .Uuid(pubnub.GetCurrentUserId())
                    .Channels(new List<PNMembership> { membership })
                    .Include(new PNMembershipField[] { PNMembershipField.CUSTOM, PNMembershipField.CHANNEL, PNMembershipField.CHANNEL_CUSTOM })
                    .IncludeCount(true)
                    .ExecuteAsync();

                PNMembershipsResult setMembershipsResult = setMembershipsResponse.Result;
                PNStatus setStatus = setMembershipsResponse.Status;
                if (setStatus.Error)
                {
                    Debug.Log($"Error when setting PN Membership: {setStatus.ErrorData}");

                }
            }
            onlineFriendsCountText.text = onlineFriendCount.ToString(); // dont include self.
            totalFriendCountText.text = totalFriendCount.ToString(); //Update at same time  
        }

        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public async void SetPlayerName()
        {
            // Set Metadata for UUID set in the pubnub instance
            PNResult<PNSetUuidMetadataResult> setUuidMetadataResponse = await pubnub.SetUuidMetadata()
                .Uuid(pubnub.GetCurrentUserId())
                .Name(playerNameInput.text)
                .ExecuteAsync();
            PNSetUuidMetadataResult setUuidMetadataResult = setUuidMetadataResponse.Result;
            PNStatus status = setUuidMetadataResponse.Status;
            if (!status.Error && setUuidMetadataResult != null)
            {
                //Update cached players name.
                PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name = playerNameInput.text;
                //Nickname is used throughout the system to define the player
                //TODO: Remove once Photon has been removed.
                PhotonNetwork.NickName = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
            }

            //Update specific gameobject if user updates while the filter list is open.          
            GameObject playerContainer = GameObject.Find(pubnub.GetCurrentUserId());
            if(playerContainer != null)
            {
                playerContainer.transform.GetChild(1).GetComponent<Text>().text = playerNameInput.text; // Update the text field
            }
        }

        // Main:
        public void FindMatch(){
            // Enable the "finding match" panel:
            findingMatchPanel.SetActive(true);
            // ...then finally, find a match:
            Connector.instance.FindMatch();
        }

        // Others:
        // *called by the toggle itself in the "On Value Changed" event:
        public void ToggleTargetFps(){
            Application.targetFrameRate = frameRateSetting.isOn? 60 : 30;

            // Display a notif message:
            if (frameRateSetting.isOn){
                DataCarrier.message = "Target frame rate has been set to 60.";
            }
        }   

        /// <summary>
        /// Open a search window that lets users search for other players.
        /// </summary>
        public void OpenSearchWindow()
        {
            //Clear list of gameobjects to manage resources.
            ClearSearchPlayersList();

            //1. Make the window active when click on button.
            if (playerListArea.activeSelf)
            {
                playerListArea.SetActive(false);
            }

            else
            {
                playerListArea.SetActive(true);
                LoadDefaultSearchPlayers();
            }
        }

        /// <summary>
        /// Gets called anytime the user is attempting to filter for players using an onchangeevent.
        /// Once users start typing, trigger onchangedevent for the nameinput
        /// </summary>
        public void FilterPlayers()
        {
            //Once event triggers, as user starts typing, clear all other users.
            ClearSearchPlayersList();

            //If completely clear search, bring back first 20 users.
            if (string.IsNullOrWhiteSpace(searchPlayersInput.text))
            {
                LoadDefaultSearchPlayers();
            }

            else
            {
                //Filter every cached player by name. Create gameobject for each of these players.
                foreach (KeyValuePair<string, UserMetadata> cachedPlayer in PNManager.pubnubInstance.CachedPlayers)
                {
                    //If users name hit a match, then add to list.
                    //Don't add own user to the list.
                    if (cachedPlayer.Value.Name.ToLowerInvariant().StartsWith(searchPlayersInput.text.ToLowerInvariant())
                        && !cachedPlayer.Value.Uuid.Equals(pubnub.GetCurrentUserId()) //lower case the text to allow for case insensitivity
                        && filteredPlayers.Find(player => player.name.Equals("search" + cachedPlayer.Value.Uuid)) == null) // don't add players to search if already friends.
                    {
                        Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                        duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                        duplciateContainer.gameObject.name = "search." + cachedPlayer.Value.Uuid;
                        duplciateContainer.gameObject.SetActive(true);
                        filteredPlayers.Add(duplciateContainer);
                    }
                }

                //If no users are matched, give a "couldn't find user" entry.
                if (filteredPlayers.Count == 0)
                {
                    Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                    duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = "No players found...";
                    duplciateContainer.Find("AddFriend").GetComponent<Button>().gameObject.SetActive(false);
                    duplciateContainer.gameObject.SetActive(true);
                    filteredPlayers.Add(duplciateContainer);
                }
            }        
        }

        /// <summary>
        /// Clears the list of searched players when trying to find new players.
        /// Used to manage resources, as well as when users are entering in new search criteria.
        /// </summary>
        public void ClearSearchPlayersList()
        {
            //Clear list of gameobjects to manage resources.
            foreach (Transform playerItem in filteredPlayers)
            {
                Destroy(playerItem.gameObject);
            }
            filteredPlayers.Clear();
        }

        /// <summary>
        /// Loads the first 20 players when searching for players (if no filter has been entered).
        /// </summary>
        public void LoadDefaultSearchPlayers()
        {
            int count = 0;
            foreach (KeyValuePair<string, UserMetadata> cachedPlayer in PNManager.pubnubInstance.CachedPlayers)
            {
                if (count > 20)
                {
                    break;
                }

                else
                {
                    //Don't add self and friends to list.
                    if(!cachedPlayer.Value.Uuid.Equals(pubnub.GetCurrentUserId())
                        && filteredPlayers.Find(player => player.name.Equals("search" + cachedPlayer.Value.Uuid)) == null)                       
                    {
                        Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                        duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                        duplciateContainer.gameObject.name = "search." + cachedPlayer.Value.Uuid;
                        duplciateContainer.gameObject.SetActive(true);
                        filteredPlayers.Add(duplciateContainer);
                        count++;
                    }                 
                }
            }
        }

        //Sets the CachedPlayer Transform's online status that is in the list with the given UserID.
        //If the IsOnline is true, then the icon will be changed to green. If false, then the icon will be changed to gray.
        public void UpdateCachedPlayerOnlineStatus(string UserID, bool IsOnline)
        {
            Transform updatePlayer = friendList.Find(player => player.name.Equals(UserID));
            if (updatePlayer != null)
            {
                updatePlayer.Find("OnlineStatus").GetComponent<Image>().color = IsOnline ? Color.green : Color.gray;

            }
        }

        /// <summary>
        /// Opens the friend list when clicking on the button.
        /// </summary>
        public void FriendListOnClick()
        {
           
            //Close the friend list if open.
            if(friendListArea.activeSelf)
            {
                friendListArea.SetActive(false);
            }

            //Open friend list.
            else
            {
                friendListArea.SetActive(true);
            }
        }

        /// <summary>
        /// Add friend when user initiates friend request.
        /// </summary>
        public void SendFriendRequestOnClick()
        {
            //obtain target user.
            string targetUser = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.name.Substring(7); //strip out the "search."

            //remove from the cached player list
            Transform playerToRemove = friendList.Find(player => player.name.Equals($"search.{targetUser}"));
            filteredPlayers.Remove(playerToRemove);
            AddFriend(targetUser);
        }

        //Remove friend when user clicks on "x".
        public void RemoveFriendOnClick()
        {
            //Remove target user from the friend list.
            string targetUser = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.name;
            RemoveFriend(targetUser);
        }

        /// <summary>
        /// Obtains the online status for friends.
        /// </summary>
        private async void GetFriendListOnlineStatus()
        {
            //Determine which friends are currently online for the buddy list.
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
               .ChannelGroups(new string[] {
                    _cgFriendList
               })
               .IncludeState(true)
               .IncludeUUIDs(true)
               .ExecuteAsync();

            PNHereNowResult herenowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;

            if (status.Error)
            {
                Debug.Log(string.Format("HereNow Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
            }
            else
            {
                int count = 0;
                //Update buddy list to show which friends are online.
                foreach (KeyValuePair<string, PNHereNowChannelData> kvp in herenowResult.Channels)
                {
                    //User ID is everything after "presence-"
                    string userId = kvp.Key.Substring(9);
                    if (!userId.Equals(pubnub.GetCurrentUserId()))
                    {
                        UpdateCachedPlayerOnlineStatus(userId, true);
                        count++;
                    }
                }
                onlineFriendsCountText.text = count.ToString(); // dont include self.
                totalFriendCountText.text = friendList.Count.ToString(); //Update at same time
            }
        }

        /// <summary>
        /// Adds the UserId to membership for client and adds the user to the buddy list.
        /// </summary>
        /// <param name="userId"></param>
        private async void AddFriend(string userId)
        {
            string targetChannel = $"presence-{userId}";

            //Add the friend to the channel group for presence updates.
            AddChannelsToGroup(_cgFriendList, new string[] {
                targetChannel
            });

            PNMembership membership = new PNMembership()
            {
                Channel = targetChannel
            };

            PNResult<PNMembershipsResult> setMembershipsResponse = await pubnub.SetMemberships()
                .Uuid(pubnub.GetCurrentUserId())
                .Channels(new List<PNMembership> { membership })
                .Include(new PNMembershipField[] { PNMembershipField.CUSTOM, PNMembershipField.CHANNEL, PNMembershipField.CHANNEL_CUSTOM })
                .IncludeCount(true)
                .ExecuteAsync();

            PNMembershipsResult setMembershipsResult = setMembershipsResponse.Result;
            PNStatus status = setMembershipsResponse.Status;
            if (status.Error)
            {
                Debug.Log($"Error when setting PN Membership: {status.ErrorData}");

            }

            else
            {
                //Add the friend to the your client buddy list if it does not exist.
                if (!friendList.Find(player => player.name.Equals(userId)))
                {
                    Transform newFriend = Instantiate(player, playerContainer);
                    newFriend.Find("PlayerUsername").GetComponent<Text>().text = PNManager.pubnubInstance.CachedPlayers[userId].Name;
                    newFriend.gameObject.name = userId;
                    newFriend.gameObject.SetActive(true);
                    friendList.Add(newFriend);
                    GetFriendListOnlineStatus();
                }
            }
        }

        /// <summary>
        /// Removes the given UserId from the client and removes the User from the buddy list.
        /// Only adds the user to the physical list of friends if from an event.
        /// </summary>
        /// <param name="userId"></param>
        private async void RemoveFriend(string userId)
        {
            string targetChannel = $"presence-{userId}";

            //Remove friend from the group for channel updates.
            RemoveChannelsFromGroup(_cgFriendList, new string[] {
                targetChannel
            });

            PNResult<PNMembershipsResult> removeMembershipsResponse = await pubnub.RemoveMemberships()
                .Uuid(pubnub.GetCurrentUserId())
                .Channels(new List<string> { targetChannel })
                .Include(new PNMembershipField[] { PNMembershipField.CUSTOM, PNMembershipField.CHANNEL, PNMembershipField.CHANNEL_CUSTOM })
                .IncludeCount(true)
                .ExecuteAsync();

            PNMembershipsResult removeMembershipsResult = removeMembershipsResponse.Result;
            PNStatus status = removeMembershipsResponse.Status;
            if (status.Error)
            {
                Debug.Log($"Error when removing PN Membership: {status.ErrorData}");

            }

            else
            {
                //Find and remove the friend from the list if it exists.
                if (friendList.Find(player => player.name.Equals(userId)) != null)
                {
                    Transform playerToRemove = friendList.Find(player => player.name.Equals(userId));
                    friendList.Remove(playerToRemove);
                    Destroy(playerToRemove.gameObject);
                    GetFriendListOnlineStatus();
                }
            }
        }

        /// <summary>
        /// Adds the specified channels to the channel group.
        /// </summary>
        /// <param name="channelGroup">The Channel Group to add the specified channels</param>
        /// <param name="channels">The channels to add to the Channel Group</param>
        public async void AddChannelsToGroup(string channelGroup, string[] channels)
        {
            PNResult<PNChannelGroupsAddChannelResult> cgAddChResponse = await pubnub.AddChannelsToChannelGroup()
               .ChannelGroup(channelGroup)
               .Channels(channels)
               .ExecuteAsync();

            //Channel groups need at least one channel.
            if (cgAddChResponse.Status.Error)
            {
                Debug.Log(string.Format("Error: statuscode: {0}, ErrorData: {1}, Category: {2}", cgAddChResponse.Status.StatusCode, cgAddChResponse.Status.ErrorData, cgAddChResponse.Status.Category));
            }
        }

        /// <summary>
        /// Removes the specified channels from the channel group.
        /// </summary>
        /// <param name="channelGroup">The Channel Group to remove the specified channesl</param>
        /// <param name="channels">The channels to remove from the Channel Group</param>
        public async void RemoveChannelsFromGroup(string channelGroup, string[] channels)
        {
            PNResult<PNChannelGroupsRemoveChannelResult> rmChFromCgResponse = await pubnub.RemoveChannelsFromChannelGroup()
                 .ChannelGroup(channelGroup)
                 .Channels(channels)
                 .ExecuteAsync();

            //Channel groups need at least one channel.
            if (rmChFromCgResponse.Status.Error)
            {
                Debug.Log(string.Format("Error: statuscode: {0}, ErrorData: {1}, Category: {2}", rmChFromCgResponse.Status.StatusCode, rmChFromCgResponse.Status.ErrorData, rmChFromCgResponse.Status.Category));
            }
        }
    }
}