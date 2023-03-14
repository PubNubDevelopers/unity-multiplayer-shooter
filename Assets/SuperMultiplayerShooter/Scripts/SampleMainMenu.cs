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
        private string _privateChannel = "private."; //Used to listen for any buddy list events, private chat, etc.
        private string _publicChannel = "public"; // Used for global presence, all chat, etc.
        private int totalFriendCount = 0;
        private PubNub _pubnub;


        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Initializes the PubNub Connection.
            _pubnub = PubNubManager.Instance.InitializePubNub();
            _privateChannel += PubNubManager.Instance.UserId; //Private channels in form of "private.<UserId>".

            //Subscribe to the list of Channels
            //Currently listening on a public channel (for presence and all chat for all users)
            //and a buddylist channel for personal user to listen for any buddy list events.
            Subscribe(new List<string>
            {
                _publicChannel,
                _privateChannel
            });

            //Obtain and cache user metadata.
            GetAllUserMetadata();

            //Loads the Initial Online Occupants and Populate Friend List.
            InitialPresenceFriendListLoad();

            //Listen for any events.
            _pubnub.SubscribeCallback += SubscribeCallbackHandler;

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
                    //Set name as first six characters of UserID for now.
                    _pubnub.SetUUIDMetadata().Name(PubNubManager.Instance.UserId.Substring(0, 6)).UUID(PubNubManager.Instance.UserId).Async((result, status) =>
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
        /// Load the Client's Friend List via Channel Members on initial load.
        /// These are other players who have been registered to a specific channel (space)
        /// when adding as friends.
        /// </summary>
        private void GetFriendList(List<PNHereNowOccupantData> onlinePlayers)
        {
            _pubnub.GetChannelMembers()
                .Channel(_privateChannel)
                .Async((result, status) =>
            {
                //Cross-reference the onlinePlayers list with that of the channel members
                if (result.Data != null)
                {
                    //The players that are online and match those in the client's members for their channel (space)
                    //will be placed in the online friends category.
                    for (int i = 0; i < result.Data.Count; i++)// (PNMembers members in result.Data)
                    {
                        //skip self
                        if (!result.Data[i].UUID.ID.Equals(PubNubManager.Instance.UserId))
                        {
                            //The color is green, which represents the user is online if there is a match.
                            Color onlineStatus = result.Data[i].UUID.ID.Equals(onlinePlayers[i].UUID) ? Color.green : Color.grey;

                            //Create player transforms for the buddy list. Can be retained and not consistenly destroyed like filtered friends.
                            Transform playerTrans = Instantiate(player, playerContainer);
                            playerTrans.Find("PlayerUsername").GetComponent<Text>().text = result.Data[i].UUID.Name;
                            playerTrans.gameObject.name = result.Data[i].ID; // Used to find the object later on.
                            playerTrans.Find("OnlineStatus").GetComponent<Image>().color = onlineStatus;
                            playerTrans.gameObject.SetActive(true);
                            friendList.Add(playerTrans);
                        }
                    }
                }

                //The client has not registered themselves for their buddy list before. Register the client as a member of their buddy list to receive presence and the removal/addition of other users..
                else
                {
                    PNChannelMembersSet input = new PNChannelMembersSet();
                    input.UUID = new PNChannelMembersUUID
                    {
                        ID = PubNubManager.Instance.UserId
                    };
                    _pubnub.SetChannelMembers()
                        .Channel(_privateChannel)
                        .Set(new List<PNChannelMembersSet> { input })
                        .Async((result, status) => {
                            Debug.Log("result.Next:" + result.Next);
                            Debug.Log("result.Prev:" + result.Prev);
                            Debug.Log("result.TotalCount:" + result.TotalCount);
                            foreach (PNMembers mem in result.Data)
                            {
                                Debug.Log(mem.UUID.ID);
                                Debug.Log(mem.UUID.Name);
                                Debug.Log(mem.UUID.Email);
                                Debug.Log(mem.UUID.ExternalID);
                                Debug.Log(mem.UUID.ProfileURL);
                                Debug.Log(mem.UUID.ID);
                                Debug.Log(mem.UUID.ETag);
                            }
                    });
                }
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
            PubNubManager.PubNub.SetUUIDMetadata().Name(playerNameInput.text).UUID(PubNubManager.Instance.UserId).Async((result, status) =>
            {
                //TODO: handle in case of errors.
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
                       // GetOnlineStatus();
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
                       // GetOnlineStatus();

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
            foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.Instance.CachedPlayers)
            {
                //If users name hit a match, then add to list.
                //Don't add own user to the list.
                if (cachedPlayer.Value.Name.ToLowerInvariant().StartsWith(searchPlayersInput.text.ToLowerInvariant())
                    && cachedPlayer.Value.ID.Equals(PubNubManager.Instance.UserId)) //lower case the text to allow for case insensitivity
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
            foreach (KeyValuePair<string, PNUUIDMetadataResult> cachedPlayer in PubNubManager.Instance.CachedPlayers)
            {
                if (count > 20)
                {
                    break;
                }

                else
                {
                    //Don't add self to list.
                    if(!cachedPlayer.Value.ID.Equals(PubNubManager.Instance.UserId))
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
            pendingPlayer.Find("PlayerUsername").GetComponent<Text>().text = PubNubManager.Instance.CachedPlayers[targetUser].Name;
            pendingPlayer.gameObject.name = "fl-" + targetUser;
            pendingPlayer.gameObject.SetActive(true);
            friendList.Add(pendingPlayer);

           // AddChannelsToGroup(cg_buddy_list, new List<string> { targetChannel });

            Dictionary<string, string> meta = new Dictionary<string, string>();
            meta.Add("invite_status", "add");

            PubNubManager.PublishMessage("Add player", targetChannel, meta);   
        }

        //Remove friend when user clicks on "x".
        public void RemoveFriendOnClick()
        {
            //Remove target user from the friend list.
            string targetUser = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.name.Substring(3); //strip out the "fl-" out of friend list.
            string targetChannel = $"ch-{targetUser}-present";
           // RemoveChannelsFromGroup(cg_buddy_list, new List<string> { targetChannel });

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
    }
}