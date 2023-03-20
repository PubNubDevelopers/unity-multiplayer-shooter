using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using PubNubAPI;
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
        private PubNub _pubnub;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Initializes the PubNub Connection.
            _pubnub = PubNubManager.Instance.InitializePubNub();
            _privateChannel += PubNubManager.Instance.UserId; //Private channels in form of "presence-<UserId>". Necessary for buddy list tracking.
            _cgFriendList += PubNubManager.Instance.UserId; //Manages the friend lists.
         
            //Obtain and cache user metadata.
            GetAllUserMetadata();

            //Add client to Channel Group just in case first time load. Will not add if already present.
            AddChannelsToGroup(_cgFriendList, new List<string> { _privateChannel });

            //Loads the Initial Online Occupants and Populate Friend List.
            InitialPresenceFriendListLoad();

            //Listen for any events.
            _pubnub.SubscribeCallback += SubscribeCallbackHandler;

            //Subscribe to the list of Channels
            //Currently listening on a public channel (for presence and all chat for all users)
            //and a buddylist channel for personal user to listen for any buddy list events.
            _pubnub.Subscribe()
               .Channels(new List<string>()
               {
                   _publicChannel,
                   _privateChannel
               })
               .ChannelGroups(new List<string>()
               {
                   _cgFriendList + "-pnpres" // Watch friends come online/go offline by watching presence events of this channel group and not message of users.
               })
               .WithPresence()
               .Execute();

            // Others:
            frameRateSetting.isOn = Application.targetFrameRate == 60;
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
        /// Called whenever the scene or game ends. Unsubscribe from all channels and channel groups to trigger events immediately.
        /// </summary>
        void OnDestroy()
        {
            _pubnub.UnsubscribeAll()
                .Async((result, status) => {
                if (status.Error)
                {
                    Debug.Log(string.Format("UnsubscribeAll Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                }
                else
                {
                    Debug.Log(string.Format("DateTime {0}, In UnsubscribeAll, result: {1}", DateTime.UtcNow, result.Message));
                }
            });
        }

        /// <summary>
        /// Obtains all player metadata from this PubNub Keyset to cache.
        /// </summary>
        private void GetAllUserMetadata()
        {
            _pubnub.GetAllUUIDMetadata().Async((result, status) => {
                //Populate Cached Players Dictionary only if they have been set previously
                if (result.Data.Count > 0)
                {
                    foreach (PNUUIDMetadataResult pnUUIDMetadataResult in result.Data)
                    {
                        PubNubManager.Instance.CachedPlayers.Add(pnUUIDMetadataResult.ID, pnUUIDMetadataResult);
                    }
                }

                //Change playerInput name to be set to username of the user as long as the name was originally set.
                if (PubNubManager.Instance.CachedPlayers.Count > 0 && PubNubManager.Instance.CachedPlayers.ContainsKey(PubNubManager.Instance.UserId))
                {
                    playerNameInput.text = PubNubManager.Instance.CachedPlayers[PubNubManager.Instance.UserId].Name;
                }
                //If current user cannot be found in cached players, then a new user is logged in. Set the metadata and add.
                else
                {
                    //Set name as UserId for now.
                    _pubnub.SetUUIDMetadata().Name(PubNubManager.Instance.UserId).UUID(PubNubManager.Instance.UserId).Async((result, status) =>
                    {
                        if (!status.Error)
                        {
                            PubNubManager.Instance.CachedPlayers.Add(result.ID, result);
                            playerNameInput.text = result.ID;
                        }

                        else
                        {
                            Debug.Log($"An error hsa occurred: {status.Error}");
                        }
                    });
                }

                //Nickname is used throughout the system to define the player
                PhotonNetwork.NickName = PubNubManager.Instance.CachedPlayers[PubNubManager.Instance.UserId].Name;
            });
        }

        /// <summary>
        /// Initial presence request for the channel name. Used for the initial total count, as well as to
        /// construct the client's friend list.
        /// </summary>
        private void InitialPresenceFriendListLoad()
        {
            _pubnub.HereNow()
                .Channels(new List<string>(){
                    _publicChannel
                })
                .IncludeState(true)
                .IncludeUUIDs(true)
                .Async((result, status) => {
                    //handle any errors
                    if (status.Error)
                    {
                        UnityEngine.Debug.Log(string.Format("HereNow Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    //Success
                    else
                    {
                        //Initial count of players in the game
                        totalCountPlayers.text = result.Channels[_publicChannel].Occupants.Count.ToString();
                        
                        //Use this list to construct the friend list for the client.
                        GetFriendList(result.Channels[_publicChannel].Occupants);
                    }
                });
        }

        /// <summary>
        /// Load the Client's Friend List via the channels associated with the client.
        /// </summary>
        private void GetFriendList(List<PNHereNowOccupantData> onlinePlayers)
        {
            int onlineFriendCount = 0;
            int totalFriendCount = 0;
            _pubnub.GetMemberships()
                .UUID(PubNubManager.Instance.UserId)
                .Async((result, status) =>
            {
                //Cross-reference the onlinePlayers list with that of the channel members
                if (result.Data != null)
                {          
                    //The players that are online and match those in the client's members for their channel (space)
                    //will be placed in the online friends category.
                    for (int i = 0; i < result.Data.Count; i++)// (PNMembers members in result.Data)
                    {
                        //Only looking for users apart of friend list.
                        if (result.Data[i].Channel.ID.StartsWith("presence"))
                        {
                            //extract user from channel name (remove the presence-)
                            string userId = result.Data[i].Channel.ID.Substring(9);
                            Color onlineStatus = Color.gray;
                            //The online status changes to green if there is a match in the onlinePlayers initial call.
                            if (onlinePlayers.Count > 0 && onlinePlayers.Find(onlineUser => onlineUser.UUID.Equals(userId)) != null)
                            {
                                onlineStatus = Color.green;
                                onlineFriendCount++;
                            }

                            //Create player transforms for the buddy list. Can be retained and not consistenly destroyed like filtered friends.
                            Transform playerTrans = Instantiate(player, playerContainer);
                            //Use the UserId in case there is no name associated with the friend.
                            playerTrans.Find("PlayerUsername").GetComponent<Text>().text = !string.IsNullOrWhiteSpace(PubNubManager.Instance.CachedPlayers[userId].Name) ? PubNubManager.Instance.CachedPlayers[userId].Name : userId;
                            playerTrans.gameObject.name = userId; // Used to find the object later on.
                            playerTrans.Find("OnlineStatus").GetComponent<Image>().color = onlineStatus;
                            playerTrans.gameObject.SetActive(true);
                            friendList.Add(playerTrans);
                            totalFriendCount++;
                        }
                    }
                }

                //The client has not registered themselves for their buddy list before. Register the client as a member of their buddy list to receive presence and the removal/addition of other users..
                else
                {
                    PNMembershipsSet inputMemberships = new PNMembershipsSet();
                    inputMemberships.Channel = new PNMembershipsChannel
                    {
                        ID = _privateChannel
                    };
                    _pubnub.SetMemberships()
                        .UUID(PubNubManager.Instance.UserId)
                        .Set(new List<PNMembershipsSet> { inputMemberships })
                        .Async((result, status) => {
                            //TODO: Handle status.
                    });
                }

                onlineFriendsCountText.text = onlineFriendCount.ToString(); // dont include self.
                totalFriendCountText.text = totalFriendCount.ToString(); //Update at same time
            });
        }

        /// <summary>
        ///  Event Handlers for listening for events on the PubNub network.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SubscribeCallbackHandler(object sender, EventArgs e)
        {
            SubscribeEventEventArgs subscribeEventEventArgs = e as SubscribeEventEventArgs;

            if (subscribeEventEventArgs.MessageResult != null)
            {
                
            }

            if (subscribeEventEventArgs.PresenceEventResult != null)
            {
                if(subscribeEventEventArgs.PresenceEventResult.Channel.Equals(_publicChannel))
                {
                    totalCountPlayers.text = subscribeEventEventArgs.PresenceEventResult.Occupancy.ToString();

                    //When user joins, check their UUID in cached players to determine if they are a new player.                 
                    if (!PubNubManager.Instance.CachedPlayers.ContainsKey(subscribeEventEventArgs.PresenceEventResult.UUID))
                    {
                        //If they do not exist, pull in their metadata (since they would have already registered when first opening app), and add to cached players.
                        _pubnub.GetUUIDMetadata()
                            .UUID(subscribeEventEventArgs.PresenceEventResult.UUID)
                            .Async((result, status) =>
                            {
                                if(result != null)
                                {
                                    PubNubManager.Instance.CachedPlayers.Add(result.ID, result);
                                }
                            });
                    }
                }

                //Friend List - Detect current friend online status. Ignore self.
                else if(subscribeEventEventArgs.PresenceEventResult.Subscription.Equals(_cgFriendList) && !PubNubManager.Instance.UserId.Equals(subscribeEventEventArgs.PresenceEventResult.UUID))
                {
                    //Friend is offline when they leave or timeout.
                    bool isOnline = !(subscribeEventEventArgs.PresenceEventResult.Event.Equals("leave") || subscribeEventEventArgs.PresenceEventResult.Event.Equals("timeout"));
                    //Search for the friend and update status
                    UpdateCachedPlayerOnlineStatus(subscribeEventEventArgs.PresenceEventResult.UUID, isOnline);
                }
            }

            //Catch other player updates (when their name changes, etc)
            if (subscribeEventEventArgs.UUIDEventResult != null)
            {
                if(PubNubManager.Instance.CachedPlayers.ContainsKey(subscribeEventEventArgs.UUIDEventResult.UUID))
                {
                    PNUUIDMetadataResult updatePlayer = PubNubManager.Instance.CachedPlayers[subscribeEventEventArgs.UUIDEventResult.UUID];
                    updatePlayer.Name = subscribeEventEventArgs.UUIDEventResult.Name;
                    updatePlayer.ExternalID = subscribeEventEventArgs.UUIDEventResult.ExternalID;
                    updatePlayer.ProfileURL = subscribeEventEventArgs.UUIDEventResult.ProfileURL;
                    //updatePlayer.ID = subscribeEventEventArgs.UUIDEventResult.UUID; Not allowing UUID changes for now.
                    updatePlayer.ETag = subscribeEventEventArgs.UUIDEventResult.ETag;
                    updatePlayer.Custom = subscribeEventEventArgs.UUIDEventResult.Custom;
                    PubNubManager.Instance.CachedPlayers[subscribeEventEventArgs.UUIDEventResult.UUID] = updatePlayer;

                    //Update friend list if the update user has updates to make.
                    //No need to update player search, as it pulls from cached players.
                    
                    Transform updateFriend = friendList.Find(player => player.name.Equals(subscribeEventEventArgs.UUIDEventResult.UUID));
                    if (updatePlayer != null)
                    {
                        updateFriend.Find("PlayerUsername").GetComponent<Text>().text = subscribeEventEventArgs.UUIDEventResult.Name;
                    }                  
                }
            }

            //Friend List - Triggerred whenever channel membership is updated (client gets added/removed from another player's friend list)
            if (subscribeEventEventArgs.MembershipEventResult != null)
            {
                //Another player has added client as a friend
                if(subscribeEventEventArgs.MembershipEventResult.ObjectsEvent.Equals(PNObjectsEvent.PNObjectsEventSet))
                {
                    AddFriend(subscribeEventEventArgs.MembershipEventResult.UUID);
                    
                }

                //Another player has removed client as a friend.
                else if (subscribeEventEventArgs.MembershipEventResult.ObjectsEvent.Equals(PNObjectsEvent.PNObjectsEventDelete))
                {
                    RemoveFriend(subscribeEventEventArgs.MembershipEventResult.UUID);        
                }
            }
        }

        /// <summary>
        /// Subscribe to the specified List of Channels.
        /// </summary>
        /// <param name="channels"></param>
        public void Subscribe(List<string> channels)
        {
            _pubnub.Subscribe()
                .Channels(channels)
                .WithPresence()
                .Execute();
        }

        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public void SetPlayerName()
        {       
            // Update metadata for current logged in user name.
            _pubnub.SetUUIDMetadata().Name(playerNameInput.text).UUID(PubNubManager.Instance.UserId).Async((result, status) =>
            {
                if(result != null)
                {
                    //Update cached players name.
                    PubNubManager.Instance.CachedPlayers[PubNubManager.Instance.UserId].Name = playerNameInput.text;
                    //Nickname is used throughout the system to define the player
                    PhotonNetwork.NickName = PubNubManager.Instance.CachedPlayers[PubNubManager.Instance.UserId].Name;
                }
            });

            //Update specific gameobject if user updates while the filter list is open.          
            GameObject playerContainer = GameObject.Find(PubNubManager.Instance.UserId);
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
                foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.Instance.CachedPlayers)
                {
                    //If users name hit a match, then add to list.
                    //Don't add own user to the list.
                    if (cachedPlayer.Value.Name.ToLowerInvariant().StartsWith(searchPlayersInput.text.ToLowerInvariant())
                        && !cachedPlayer.Value.ID.Equals(PubNubManager.Instance.UserId) //lower case the text to allow for case insensitivity
                        && filteredPlayers.Find(player => player.name.Equals("search"+cachedPlayer.Value.ID)) == null) // don't add players to search if already friends.
                    {
                        Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                        duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                        duplciateContainer.gameObject.name = "search." + cachedPlayer.Value.ID;
                        duplciateContainer.gameObject.SetActive(true);
                        filteredPlayers.Add(duplciateContainer);
                    }
                }

                //If no users are matched, give a "couldn't find user" entry.
                if (filteredPlayers.Count == 0)
                {
                    Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                    duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = "No players found that match this description...";
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
            foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.Instance.CachedPlayers)
            {
                if (count > 20)
                {
                    break;
                }

                else
                {
                    //Don't add self and friends to list.
                    if(!cachedPlayer.Value.ID.Equals(PubNubManager.Instance.UserId)
                        && filteredPlayers.Find(player => player.name.Equals("search" + cachedPlayer.Value.ID)) == null)                       
                    {
                        Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                        duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                        duplciateContainer.gameObject.name = "search." + cachedPlayer.Value.ID;
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
        private void GetFriendListOnlineStatus()
        {
            //Determine which friends are currently online for the buddy list.
            _pubnub.HereNow()
                //Obtain information about the currently logged in users.          
                .ChannelGroups(new List<string> { _cgFriendList })
                .IncludeState(true)
                .IncludeUUIDs(true)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("HereNow Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    else
                    {
                        int count = 0;
                        //Update buddy list to show which friends are online.
                        foreach (KeyValuePair<string, PNHereNowChannelData> kvp in result.Channels)
                        {
                            //User ID is everything after "presence-"
                            string userId = kvp.Key.Substring(9);
                            if (!userId.Equals(PubNubManager.Instance.UserId))
                            {
                                UpdateCachedPlayerOnlineStatus(userId, true);
                                count++;
                            }
                        }
                        onlineFriendsCountText.text = count.ToString(); // dont include self.
                        totalFriendCountText.text = friendList.Count.ToString(); //Update at same time
                    }
                    Debug.Log(status.Error);
                });
        }

        /// <summary>
        /// Adds the UserId to membership for client and adds the user to the buddy list.
        /// </summary>
        /// <param name="userId"></param>
        private void AddFriend(string userId)
        {
            string targetChannel = $"presence-{userId}";

            //Add the friend to the channel group for presence updates.
            AddChannelsToGroup(_cgFriendList, new List<string>() {
                targetChannel
            });
            //Set the targeted player as a channel member of the client's friend list channel (_friendListChannel).
            PNMembershipsSet inputMemberships = new PNMembershipsSet();
            inputMemberships.Channel = new PNMembershipsChannel
            {
                ID = targetChannel
            };

            //add membershipt to trigger memebership event and allow other users to update their own friend lists.
            _pubnub.SetMemberships()
                .UUID(PubNubManager.Instance.UserId)
                .Set(new List<PNMembershipsSet> { inputMemberships })
                .Async((result, status) => {
                    //Add the friend to the your client buddy list if it does not exist.
                    if(!friendList.Find(player => player.name.Equals(userId)))
                    {
                        Transform newFriend = Instantiate(player, playerContainer);
                        newFriend.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.Instance.CachedPlayers[userId].Name;
                        newFriend.gameObject.name = userId;
                        newFriend.gameObject.SetActive(true);
                        friendList.Add(newFriend);
                        GetFriendListOnlineStatus();
                    }      
                });
        }

        /// <summary>
        /// Removes the given UserId from the client and removes the User from the buddy list.
        /// Only adds the user to the physical list of friends if from an event.
        /// </summary>
        /// <param name="userId"></param>
        private void RemoveFriend(string userId)
        {
            string targetChannel = $"presence-{userId}";

            //Remove friend from the group for channel updates.
            RemoveChannelsFromGroup(_cgFriendList, new List<string>() {
                targetChannel
            });
            PNMembershipsRemove inputMembershipsRm = new PNMembershipsRemove();
            inputMembershipsRm.Channel = new PNMembershipsChannel
            {
                ID = targetChannel
            };
            //Remove membership to trigger membership event and allow other users to update their own friend lists.
            _pubnub.RemoveMemberships()
                .UUID(PubNubManager.Instance.UserId)
                .Remove(new List<PNMembershipsRemove> { inputMembershipsRm })
                .Async((result, status) => {
                    //Find and remove the friend from the list if it exists.
                    if(friendList.Find(player => player.name.Equals(userId)) != null)
                    {
                        Transform playerToRemove = friendList.Find(player => player.name.Equals(userId));
                        friendList.Remove(playerToRemove);
                        Destroy(playerToRemove.gameObject);
                        GetFriendListOnlineStatus();
                    }                  
                });
        }

        //Adds the specified channels to the channel group.
        public void AddChannelsToGroup(string channelGroup, List<string> channels)
        {
            _pubnub.AddChannelsToChannelGroup()
                .Channels(channels)
                .ChannelGroup(channelGroup)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        //Channel groups need at least one channel.
                        Debug.Log(string.Format("Error: statuscode: {0}, ErrorData: {1}, Category: {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    //success. 
                    else
                    {
                        Debug.Log($"Result: {result.Message}");
                        //Update Total Friend Count
                        //Update online status.

                        //Increase friend count
                        //TODO: Might change this to using list channel groups to get accurate info.
                        //int numTotalFriends = 0;
                        //if (int.TryParse(totalFriendCountText.text, out numTotalFriends))
                        
                        //    totalFriendCount += 1;
                        //}
                        //GetOnlineStatus();
                    }
                });
        }

        //Removes the specified channels from the channel group.
        public void RemoveChannelsFromGroup(string channelGroup, List<string> channels)
        {
           _pubnub.RemoveChannelsFromChannelGroup()
                .Channels(channels)
                .ChannelGroup(channelGroup)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        //Channel groups need at least one channel.
                        Debug.Log(string.Format("Error: statuscode: {0}, ErrorData: {1}, Category: {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    //success. 
                    else
                    {
                        Debug.Log($"Result: {result.Message}");        
                    }
            });
        }
    }
}