using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using PubNubAPI;
using Newtonsoft.Json;

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
        public List<Transform> filteredPlayers;

        //Friend List
        public Button friendListBtn;
        public Text totalFriendCountText; //total number of friends user has, both online/offline.
        public Text onlineFriendsCountText; //number of friends that are currently online.
        public GameObject friendListArea;
        public Transform playerContainer;
        public Transform player;
        private List<Transform> friendList = new List<Transform>();

        //Helper properties
        public string cg_buddy_list = $"cg-friends";
        private int totalFriendCount = 0;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Initialize PubNub Configuration
            PNConfiguration pnConfiguration = new PNConfiguration();
            pnConfiguration.SubscribeKey = "sub-c-68a629b6-9566-4f83-b74b-000b0fad8a69";
            pnConfiguration.PublishKey = "pub-c-27c9a7a7-bbb0-4210-8dd7-850512753b31";
            pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;

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

            pnConfiguration.UserId = uuid;
            PubNubManager.PubNub = new PubNub(pnConfiguration);

            //Channel used for the main-menu: All-chat, Presence, Buddy List
            string ch_main_menu = "main-menu"; 

            //For Friend Lists, create presence channels to indicate online status and will be unique per user.
            string ch_online_status = $"ch-{uuid}-present";
          
            //Add channel to friends list (channel group). Add even if already previously added/exists.
            cg_buddy_list = $"cg-{uuid}-friends";
            AddChannelsToGroup(cg_buddy_list, new List<string> { ch_online_status });

           //RemoveChannelsFromGroup(cg_buddy_list, new List<string> { $"ch-User#2995-channel" });
           // RemoveChannelsFromGroup($"cg-User#2995-friends", new List<string> { $"ch-{uuid}-channel" });
           // RemoveChannelsFromGroup($"cg-User#2995-friends", new List<string> { $"ch-User#2995-channel" });



            //Set memberships for existing user. Won't overwrite existing channels even if set multiple times.
            //Needed to invoke SubscribeEventEventArgs.UUIDEventResult calls whenever an object is updated.
            PNMembershipsSet inputMemberships = new PNMembershipsSet();
            inputMemberships.Channel = new PNMembershipsChannel
            {
                ID = ch_main_menu
            };

            //Register the user
            PubNubManager.PubNub.SetMemberships().UUID(PubNubManager.PubNub.PNConfig.UserId).Set(new List<PNMembershipsSet> { inputMemberships }).Async((result, status) => {
                if (status.Error)
                {
                    Debug.Log("Error when setting membership: " + status.ErrorData.ToString());
                    //TODO: Retry in case of error.
                }
            });

            //Pull all player metadata from this PubNub keyset. Save to a cached Dictionary to reference later on.
            PubNubManager.PubNub.GetAllUUIDMetadata().Async((result, status) => {
                //result.Data is a List<PUUIDMetadataResult>. Details here https://www.pubnub.com/docs/sdks/unity/api-reference/objects#pnuuidmetadataresult
                if (result.Data.Count > 0)
                {
                    //Store players in a cached Dictionary to reference later on.
                    foreach (PNUUIDMetadataResult pnUUIDMetadataResult in result.Data)
                    {
                        PubNubManager.CachedPlayers.Add(pnUUIDMetadataResult.ID, pnUUIDMetadataResult);
                    }
                }
                //Change playerInput name to be set to username of the user as long as the name was originally set.
                if (PubNubManager.CachedPlayers != null && PubNubManager.CachedPlayers.ContainsKey(PubNubManager.PubNub.PNConfig.UserId))
                {
                    playerNameInput.text = PubNubManager.CachedPlayers[PubNubManager.PubNub.PNConfig.UserId].Name;
                }
                //If current user cannot be found in cached players, then a new user is lgoged in. Set the metadata and add.
                else
                {
                    //Set name as first six characters of UserID for now.
                    PubNubManager.PubNub.SetUUIDMetadata().Name(PubNubManager.PubNub.PNConfig.UserId.Substring(0, 6)).UUID(PubNubManager.PubNub.PNConfig.UserId).Async((result, status) =>
                    {
                        if (!status.Error)
                        {
                            PubNubManager.CachedPlayers.Add(result.ID, result);
                            playerNameInput.text = result.ID;
                        }

                        else
                        {
                            //TODO: handle in case of errors.
                        }
                    });
                }
            });

            //Load Friend List Players
            RefreshFriendList();
                     
            //Listen for any new incoming messages, presence, object, and state changes.
            PubNubManager.PubNub.SubscribeCallback += (sender, e) => {
                SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;           
                if (mea.MessageResult != null)
                {                   
                    //Extract any metadata from the message publish.
                    //Used to test in debug console.
                    if(mea.MessageResult.UserMetadata != null)
                    {
                        var metaDataJSON = JsonConvert.SerializeObject(mea.MessageResult.UserMetadata);
                        string status = "";
                        var metaDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(metaDataJSON);
                        if(metaDataDictionary.TryGetValue("invite_status", out status))
                        {
                            string ch_target_channel = $"ch-{mea.MessageResult.IssuingClientId}-present";
                            if (status.Equals("add"))
                            {
                                Transform pendingPlayer = Instantiate(player, playerContainer);
                                pendingPlayer.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.CachedPlayers[mea.MessageResult.IssuingClientId].Name;
                                pendingPlayer.gameObject.name = "fl-" + mea.MessageResult.IssuingClientId;
                                pendingPlayer.gameObject.SetActive(true);
                                friendList.Add(pendingPlayer);
                                AddChannelsToGroup(cg_buddy_list, new List<string> { ch_target_channel });
                            }

                            //User removed friend
                            else if (status.Equals("remove"))
                            {

                                //Remove ch-<user>-present from cg-buddy-list.
                                RemoveChannelsFromGroup(cg_buddy_list, new List<string>() { ch_target_channel });

                                //Find and remove the friend from the list.
                                Transform playerToRemove = friendList.Find(player => player.name.Equals("fl-" + mea.MessageResult.IssuingClientId));
                                friendList.Remove(playerToRemove);
                                Destroy(playerToRemove.gameObject);
                            }
                        }

                    }
                    /*
                    var metaDataJSON = JsonConvert.SerializeObject(mea.MessageResult.UserMetadata);
                    string username = "";
                    var metaDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(metaDataJSON);
                    //If cannot find the metadata, grab first six chars of user id. If the user id is also blank, set back-up as guest.
                    if (metaDataDictionary.TryGetValue("name", out username))
                    {
                        //if(!PubNubManager.CachedPlayers.ContainsKey(mea.MessageResult.IssuingClientId))
                        //{
                            
                            //PNMembershipsSet inputMemberships = new PNMembershipsSet();
                            //inputMemberships.Channel = new PNMembershipsChannel
                            //{
                            //    ID = "menu"
                            //};
                        PubNubManager.PubNub.SetMemberships().UUID(mea.MessageResult.IssuingClientId).Set(new List<PNMembershipsSet> { inputMemberships }).Async((result, status) => {
                        //name has changed. update
                        if (!PubNubManager.CachedPlayers.ContainsKey(mea.MessageResult.IssuingClientId) || !username.Equals(PubNubManager.CachedPlayers[mea.MessageResult.IssuingClientId].Name))
                        {
                            PubNubManager.PubNub.SetUUIDMetadata().Name(username).UUID(mea.MessageResult.IssuingClientId).Async((result, status) =>
                            {
                                //TODO: handle in case of errors.
                                //
                                PubNubManager.CachedPlayers.Add(result.ID, result);
                            });
                        }
                            });
                       //}
                        
                    }
                    */
                    string test = "hi";
                }

                //Used to catch the online/offline status of friends and users.
                if (mea.PresenceEventResult != null)
                {
                    //Update the active game player count if a user joins, leaves, or timesout.
                    if(mea.PresenceEventResult.Subscription.Equals(ch_main_menu))
                    {
                        int occupancy = mea.PresenceEventResult.Occupancy;
                        totalCountPlayers.text = mea.PresenceEventResult.Occupancy.ToString();
                    }
                    /*
                    //Handle any buddy list updates.
                    //TODO: Bug that is triggerring presence events for same client when remove a different user from the list. Meant for other users to handle.
                    else if(mea.PresenceEventResult.Subscription.Equals(cg_buddy_list) && !mea.PresenceEventResult.UUID.Equals(PubNubManager.PubNub.PNConfig.UserId))
                    {
                        //Updates here for join or leave/timeout.
                        //Update the online count of friends in the buddy list regardless of the event.
                        onlineFriendsCountText.text = mea.PresenceEventResult.Occupancy.ToString(); //TODO: Make sure this works correctly with multiple friends.
                                                                    
                        //Depending on event, the player will need to be added to the friend list or removed if a state change was triggerred.
                        if (mea.PresenceEventResult.Event.Equals("state-change") && mea.PresenceEventResult.State != null)
                        {
                            var metaDataJSON = PubNubManager.PubNub.JsonLibrary.SerializeToJsonString(mea.PresenceEventResult.State);
                            object inviteStatus = "";
                            bool statusExtracted = false;

                            var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaDataJSON);
                            //Need to obtain the invite_status value. Parse the value out.
                            foreach (KeyValuePair<string, object> kvp in metadata)
                            {
                                var stateJSON = PubNubManager.PubNub.JsonLibrary.SerializeToJsonString(kvp.Value);
                                var statePairs = JsonConvert.DeserializeObject<Dictionary<string, object>>(stateJSON);
                                statusExtracted = statePairs.TryGetValue("invite_status", out inviteStatus);
                                if (statusExtracted)
                                {
                                    break;
                                }
                            }

                            if(statusExtracted)
                            {
                                string ch_target_channel = $"ch-{mea.PresenceEventResult.UUID}-present";
                                //The target user initiated a friend request. Add to cg-buddy-list and to Friend List UI.
                                if (inviteStatus.Equals("add"))
                                {
                                    Transform pendingPlayer = Instantiate(player, playerContainer);
                                    pendingPlayer.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.CachedPlayers[mea.PresenceEventResult.UUID].Name;
                                    pendingPlayer.gameObject.name = "fl-" + mea.PresenceEventResult.UUID;
                                    //pendingPlayer.Find("OnlineStatus").GetComponent<Image>().color = Color.green;
                                    pendingPlayer.gameObject.SetActive(true);
                                    friendList.Add(pendingPlayer);
                                    AddChannelsToGroup(cg_buddy_list, new List<string> { ch_target_channel });                                                                    
                                }

                                //User removed friend
                                else if (inviteStatus.Equals("remove"))
                                {                         
                                   
                                    //Remove ch-<user>-present from cg-buddy-list.
                                    RemoveChannelsFromGroup(cg_buddy_list, new List<string>() { ch_target_channel });

                                    //Find and remove the friend from the list.
                                    Transform playerToRemove = friendList.Find(player => player.name.Equals("fl-" + mea.PresenceEventResult.UUID));
                                    friendList.Remove(playerToRemove);
                                    Destroy(playerToRemove.gameObject);                                   
                                }                           
                            }
                        }

                        else
                        {
                            bool isOnline = mea.PresenceEventResult.Event.Equals("join"); //online = join, offline = leave or timeout. TODO: What about interval mode?
                            UpdateCachedPlayerOnlineStatus("fl-" + mea.PresenceEventResult.UUID, isOnline); //Update friend list icon.   
                        }                               
                    }  */         
                }

                //Whenever metadata is updated (username, etc), update local cached source.
                //Note: Does not trigger for our own updated username.
                if (mea.UUIDEventResult != null)
                {
                    //Player should already exist, unless their UUID was changed.
                    if (PubNubManager.CachedPlayers.ContainsKey(mea.UUIDEventResult.UUID))
                    {
                        //Update cached with new information
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].Name = mea.UUIDEventResult.Name;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].Email = mea.UUIDEventResult.Email;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].ExternalID = mea.UUIDEventResult.ExternalID;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].ETag = mea.UUIDEventResult.ETag;
                    }

                    //Update user when their UUID has changed. OR when a new metadata is created?
                    else
                    {
                        //TODO: Implement update when a user has changed their UUID.
                    }
                }               
            };

            //Subscribe to the lobby chat channel
            PubNubManager.PubNub.Subscribe()
               .Channels(new List<string>(){
                    ch_main_menu,
                    ch_online_status
               })
               .ChannelGroups(new List<string>()
               {
                   cg_buddy_list + "-pnpres" // Watch friends come online/go offline by watching presence events of this channel group and not message of users.
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

        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public void SetPlayerName()
        {
            //TODO: Update to remove nickname from this section.
            PhotonNetwork.NickName = playerNameInput.text;
            // Update metadata for current logged in user name.
            PubNubManager.PubNub.SetUUIDMetadata().Name(playerNameInput.text).UUID(PubNubManager.PubNub.PNConfig.UserId).Async((result, status) =>
            {
                //TODO: handle in case of errors.
            });

            //Update specific gameobject if user updates while the filter list is open.          
            GameObject playerContainer = GameObject.Find(PubNubManager.PubNub.PNConfig.UserId);
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

        //Adds the specified channels to the channel group.
        public void AddChannelsToGroup(string channelGroup, List<string>channels)
        {
            PubNubManager.PubNub.AddChannelsToChannelGroup()
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
                        int numTotalFriends = 0;
                        if (int.TryParse(totalFriendCountText.text, out numTotalFriends))
                        {
                            totalFriendCount += 1;
                        }
                        GetOnlineStatus();
                    }
                });
        }

        //Removes the specified channels from the channel group.
        public void RemoveChannelsFromGroup(string channelGroup, List<string> channels)
        {
            PubNubManager.PubNub.RemoveChannelsFromChannelGroup()
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
                 
                        //Update total count.
                        //TODO: Might change this to using list channel groups to get accurate info.
                        int numTotalFriends = 0;

                        if (int.TryParse(totalFriendCountText.text, out numTotalFriends))
                        {
                            totalFriendCount -= 1;
                        }
                        //Update online status
                        GetOnlineStatus();

                    }
                });
        }

        //Open a search window that lets users search for other players.
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

        //Gets called anytime the user is attempting to filter for players using an onchangeevent.
        //Once users start typing, trigger onchangedevent for the nameinput
        public void FilterPlayers()
        {
            //Once event triggers, as user starts typing, clear all other users.
            ClearSearchPlayersList();

            //If completely clear search, bring back first 20 users.
            if (string.IsNullOrWhiteSpace(searchPlayersInput.text))
            {
                LoadDefaultSearchPlayers();
            }

            //Filter every cached player by name. Create gameobject for each of these players.
            foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.CachedPlayers)
            {
                //If users name hit a match, then add to list.
                //Don't add own user to the list.
                if (cachedPlayer.Value.Name.ToLowerInvariant().StartsWith(searchPlayersInput.text.ToLowerInvariant())
                    && cachedPlayer.Value.ID.Equals(PubNubManager.PubNub.PNConfig.UserId)) //lower case the text to allow for case insensitivity
                {                  
                    Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                    duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                    duplciateContainer.gameObject.name = cachedPlayer.Value.ID;
                    duplciateContainer.gameObject.SetActive(true);
                    filteredPlayers.Add(duplciateContainer);                                  
                }              
            }

            //If users are matched, give a "couldn't find user" entry.
            if (filteredPlayers.Count == 0)
            {
                Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = "No players found that match this description...";
                duplciateContainer.gameObject.SetActive(true);
                filteredPlayers.Add(duplciateContainer);
            }
        }

        //Clears the list of searched players when trying to find new players.
        //Used to manage resources, as well as when users are entering in new search criteria.
        public void ClearSearchPlayersList()
        {
            //Clear list of gameobjects to manage resources.
            foreach (Transform playerItem in filteredPlayers)
            {
                Destroy(playerItem.gameObject);
            }
            filteredPlayers.Clear();
        }

        //Loads the first 20 players when searching for players (if no filter has been entered).
        public void LoadDefaultSearchPlayers()
        {
            int count = 0;
            foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.CachedPlayers)
            {
                if (count > 20)
                {
                    break;
                }

                else
                {
                    //Don't add self to list.
                    if(!cachedPlayer.Value.ID.Equals(PubNubManager.PubNub.PNConfig.UserId))
                    {
                        Transform duplciateContainer = Instantiate(searchPlayer, searchPlayerContent);
                        duplciateContainer.Find("PlayerUsername").GetComponent<Text>().text = cachedPlayer.Value.Name;
                        duplciateContainer.gameObject.name = cachedPlayer.Value.ID;
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

        public void RefreshFriendList()
        {            
            //Get the List of Friends when first load app.
            PubNubManager.PubNub.ListChannelsForChannelGroup()
                .ChannelGroup(cg_buddy_list)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("In Example, ListAllChannelsOfGroup Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    else
                    {
                        //Update friend List count, which is represented by number of people added to the friend list.
                        //Total count of channels is total -1 (don't include self for buddy). HereNow will be used to obtain initial online friends count.
                        //Presence to be used to track when friends come online/offline to update.
                        totalFriendCount = result.Channels.Count - 1;

                        //Extract UserIDs from channel names and create transforms. Add to friend List.
                        foreach (string presenceChannel in result.Channels)
                        {                           
                            //User ID is everything inbetween ch- and -present.
                            string userId = presenceChannel.Substring(3, presenceChannel.Length - 11);
                          
                            //Do not include current logged in player in buddy list.
                            if (!PubNubManager.PubNub.PNConfig.UserId.Equals(userId))
                            {
                                //Create player transforms for the buddy list. Can be retained and not consistenly destroyed like filtered friends.
                                Transform playerTrans = Instantiate(player, playerContainer);
                                playerTrans.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.CachedPlayers[userId].Name;                                             
                                playerTrans.gameObject.name = "fl-" + userId; //TODO: necessary to append fl-? differentiate between search users and friend list.
                                playerTrans.gameObject.SetActive(true);
                                friendList.Add(playerTrans);
                            }
                        }
                        //Update online status.
                        GetOnlineStatus();
                    }                
                });
        }

        //Opens the friend list when clicking on the button.
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

        //Add friend when user initiates friend request.
        public void SendFriendRequestOnClick()
        {
            //Initiate friend request with that specified user.
            string targetUser = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.name;
            string targetChannel = $"ch-{targetUser}-present";

            //Add pending player to buddy list.
            Transform pendingPlayer = Instantiate(player, playerContainer);
            pendingPlayer.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.CachedPlayers[targetUser].Name;
            pendingPlayer.gameObject.name = "fl-" + targetUser;
            pendingPlayer.gameObject.SetActive(true);
            friendList.Add(pendingPlayer);

            AddChannelsToGroup(cg_buddy_list, new List<string> { targetChannel });

            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta.Add("invite_status", "add");

            PubNubManager.PublishMessage("Add player", targetChannel, meta);

            /*
            PubNubManager.PubNub.Publish()
                .Channel(targetChannel)
                .Message("Adding friend")
                .Meta(meta)
                .Async((result, status) => {
                    if (!status.Error)
                    {
                        //Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow, result.Timetoken));
                    }
                    else
                    {
                        Debug.Log(status.Error);
                        Debug.Log(status.ErrorData.Info);
                    }
                });
            */
            //Update with new state to trigger a state-change Presence Event to be detected by that designated friend.
            //state.Add("invite_status", "add");
            /*
            PubNubManager.PubNub.SetPresenceState()
                .ChannelGroups(new List<string>() { cg_ })
                .State(state)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("In Example, SetPresenceState Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    //Success
                    else
                    {
                        //TODO: Handle success.
                    }
                });
            */
        }

        //Remove friend when user clicks on "x".
        public void RemoveFriendOnClick()
        {
            //Remove target user from the friend list.
            string targetUser = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.name.Substring(3); //strip out the "fl-" out of friend list.
            string targetChannel = $"ch-{targetUser}-present";
            RemoveChannelsFromGroup(cg_buddy_list, new List<string> { targetChannel });

            //Find and remove the friend from the list.
            Transform playerToRemove = friendList.Find(player => player.name.Equals("fl-" + targetUser));
            friendList.Remove(playerToRemove);
            Destroy(playerToRemove.gameObject);

            //Send a message to update with changes.
            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta.Add("invite_status", "remove");
            PubNubManager.PublishMessage("Remove player", targetChannel, meta);
            /*
            PubNubManager.PubNub.Publish()
                .Channel(targetChannel)
                .Message("Removing friend")
                .Meta(meta)
                .Async((result, status) => {
                    if (!status.Error)
                    {
                        //Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow, result.Timetoken));
                    }
                    else
                    {
                        Debug.Log(status.Error);
                        Debug.Log(status.ErrorData.Info);
                    }
                });*/
            /*
            //Update with new state to trigger a state-change Presence Event to be detected by that designated friend.
            Dictionary<string, object> state = new Dictionary<string, object>();
            state.Add("invite_status", "remove");
            PubNubManager.PubNub.SetPresenceState()
                .ChannelGroups(new List<string>() { cg_buddy_list })
                .State(state)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("In Example, SetPresenceState Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    //Success
                    else
                    {
                        //TODO: Handle success.
                    }
                });*/
        }

        public void GetOnlineStatus()
        {
            //Determine which friends are currently online for the buddy list.
            PubNubManager.PubNub.HereNow()
                //Obtain information about the currently logged in users.          
                .ChannelGroups(new List<string>()
                {
                    cg_buddy_list
                })
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
                        foreach(KeyValuePair<string, PNHereNowChannelData> kvp in result.Channels)
                        {
                            //User ID is everything inbetween ch- and -present.
                            string userId = kvp.Key.Substring(3, kvp.Key.Length - 11);
                            if(!userId.Equals(PubNubManager.PubNub.PNConfig.UserId))
                            {
                                UpdateCachedPlayerOnlineStatus("fl-" + userId, true);
                                count++;
                            }
                        }
                        onlineFriendsCountText.text = count.ToString(); // dont include self.
                        totalFriendCountText.text = totalFriendCount.ToString(); //Update at same time
                    }
                    Debug.Log(status.Error);
                });
        }
    }
}