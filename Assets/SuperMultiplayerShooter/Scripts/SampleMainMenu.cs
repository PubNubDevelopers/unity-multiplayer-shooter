using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PubnubApi;
using PubnubApi.Unity;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

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
        public Text totalCountPlayers;
        public Image onlineStatus;
        public GameObject chat;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Add Listeners
            Connector.instance.OnGlobalPlayerCountUpdate += UpdateGlobalPlayers;
            Connector.instance.OnConnectorReady += ConnectorReady;
            Connector.instance.onPubNubObject += OnPnObject;
        }

        /// <summary>
        /// Watch for any presence event changes by subscribing to Connector
        /// </summary>
        /// <param name="count">The global number of players in the game</param>
        private void UpdateGlobalPlayers(int count)
        {
            totalCountPlayers.text = count.ToString();
        }

        /// <summary>
        /// Watch for when the connector is ready.
        /// </summary>
        private void ConnectorReady()
        {
            //Sets up the Main Menu based on player cached information.
            MainMenuSetup();
        }
    
        /// <summary>
        /// Event listener to handle PubNub Object events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnObject(PNObjectEventResult result)
        {
            //Catch other player updates (when their name changes, etc)
            if (result != null)
            {
                //User Metadata Update from another player, such as a name change. Update chached player
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

                    // Add new or update existing player
                    if(result.Event.Equals("set"))
                    {
                        if(PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.UuidMetadata.Uuid))
                        {
                            PNManager.pubnubInstance.CachedPlayers[result.UuidMetadata.Uuid] = meta;
                        }

                        else
                        {
                            PNManager.pubnubInstance.CachedPlayers.Add(result.UuidMetadata.Uuid, meta);
                        }
                    }

                    // Remove player from cache
                    else if (result.Event.Equals("delete") && PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.UuidMetadata.Uuid))
                    {
                        PNManager.pubnubInstance.CachedPlayers.Remove(result.UuidMetadata.Uuid);
                    }
                }
                /*
                // TODO: Handle Friend List Changes - Triggerred whenever channel membership is updated (client gets added/removed from another player's friend list)
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
                */
            }
        }
      
        // Update is called once per frame
        void Update()
        {
            // Handling panels:
            customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);

            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }

            //Listen for whenever user opens the chat window.
            if (chat.activeSelf == false && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                //opens the chat window.
                chat.SetActive(true);
            }
        }

        /// <summary>
        /// Called whenever the scene or game ends. Unsubscribe event listeners.
        /// </summary>
        void OnDestroy()
        {
            Connector.instance.OnGlobalPlayerCountUpdate -= UpdateGlobalPlayers;
            Connector.instance.OnConnectorReady -= ConnectorReady;
            Connector.instance.onPubNubObject -= OnPnObject;

            //Clear out the cached players when changing scenes. The list needs to be updated when returning to the scene in case
            //there are new players.
            PNManager.pubnubInstance.CachedPlayers.Clear();
        }

        /// <summary>
        /// Extracts metadata for active user and performs menu setup.
        /// </summary>
        private void MainMenuSetup()
        {          
            //Change playerInput name to be set to username of the user as long as the name was originally set.
            if (PNManager.pubnubInstance.CachedPlayers.Count > 0 && PNManager.pubnubInstance.CachedPlayers.ContainsKey(Connector.instance.GetPubNubObject().GetCurrentUserId()))
            {
                playerNameInput.text = Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Name;
                //  Populate the available hat inventory and other settings, read from PubNub App Context
                Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom;
                if (customData != null)
                {
                    List<int> availableHats = new List<int>();
                    if (customData.ContainsKey("hats"))
                    {
                        availableHats = JsonConvert.DeserializeObject<List<int>>(customData["hats"].ToString());
                    }

                    // Users should always have hats should never be null - catch legacy user situations.
                    else
                    {
                        availableHats = Connector.instance.GenerateRandomHats();
                        customData.Add("hats", availableHats);
                        PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom = customData;
                    }

                    Connector.instance.UpdateAvailableHats(availableHats);

                    if (customData.ContainsKey("language"))
                    {
                        Connector.UserLanguage = customData["language"].ToString();
                    }

                    if (customData.ContainsKey("60fps"))
                    {
                        bool result = false;
                        bool.TryParse(customData["60fps"].ToString(), out result);
                        Connector.IsFPSSettingEnabled = result;
                    }
                }
            }                        
        }

        /*
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

                    //Use this list to construct the friend list for the client.
                    //GetFriendList(herenowResult.Channels[_publicChannel].Occupants);
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
        */
        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public async void SetPlayerName()
        {
            await PNManager.pubnubInstance.UpdateUserMetadata(Connector.instance.GetPubNubObject().GetCurrentUserId(), playerNameInput.text, PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom);
            Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Name = playerNameInput.text;     
        }

        // Main:
        public void FindMatch(){
            // Enable the "finding match" panel:
            findingMatchPanel.SetActive(true);
            //  Matchmaking has been removed for simplicity
        }
        /*
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
        */
    
  
    }
}