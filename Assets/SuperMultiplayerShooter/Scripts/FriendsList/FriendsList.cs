using Newtonsoft.Json;
using PubnubApi;
using PubNubUnityShowcase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class FriendsList : MonoBehaviour
{
    //UI Fields
    public Transform friendsListItemHandler;       // this is where the room item prefabs will be spawned
    public FriendsListItem friendsListItemPrefab;         // the room item prefab (represents a game session in the lobby list)
    public Button searchFriendsButton;
    public GameObject privateMessagePopupPanel;

    // Start is called before the first frame update
    async void Start()
    {
        //Listeners
        Connector.instance.OnPlayerSelect += AddFriend;
        Connector.instance.onPubNubMessage += OnPnMessage;
        Connector.instance.onPubNubPresence += OnPnPresence;
        Connector.instance.onPubNubObject += OnPnObject;

        // Testing - add self presence channel to channel group. presence channel to each channel group (online status and messages)
        //await PNManager.pubnubInstance.DeleteChannelGroup(PubNubUtilities.chanFriendChanGroupStatus + Connector.instance.GetPubNubObject().GetCurrentUserId());
        //await PNManager.pubnubInstance.DeleteChannelGroup(PubNubUtilities.chanFriendChanGroupChat + Connector.instance.GetPubNubObject().GetCurrentUserId());
        //await PNManager.pubnubInstance.AddChannelsToChannelGroup("cg_user124", new string[] { "chats.room1", "chats.room2", "alerts.system" });

        // Since both online status and message channel groups are in conjunction, doesn't matter which to use.
        //Populate friend group
        await PopulateFriendList(PubNubUtilities.chanFriendChanGroupStatus + Connector.instance.GetPubNubObject().GetCurrentUserId());
        // Get Online Status of Friends by referencing who's currently online.
        await GetCurrentFriendOnlineStatus();
    }

    public void OnDestroy()
    {
        Connector.instance.OnPlayerSelect -= AddFriend;
        Connector.instance.onPubNubMessage -= OnPnMessage;
        Connector.instance.onPubNubPresence -= OnPnPresence;
        Connector.instance.onPubNubObject -= OnPnObject;
    }

    /// <summary>
    /// Listen for message events. Handling only friend requests, friend chat messages are handled in Chat.cs.
    /// Ignore these requests from yourself.
    /// </summary>
    /// <param name="result"></param>
    private async void OnPnMessage(PNMessageResult<object> result)
    {
        if(result != null && !string.IsNullOrWhiteSpace(result.Message.ToString())
            && !string.IsNullOrWhiteSpace(result.Channel) && (result.Channel.StartsWith(PubNubUtilities.chanFriendRequest))
            && PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.Publisher) 
            && !PNManager.pubnubInstance.CachedPlayers[result.Publisher].Equals(Connector.instance.GetPubNubObject().GetCurrentUserId()))
        {
            //Handle request based on the message.
            switch(result.Message)
            {
                //Another user has initiated a friend request. Display user as temporary friend until you accept/deny.
                // Friend Request Cycle: request -> accept -> become friends (add to channel group) -> cycle complete (delete messages)
                //                               -> reject -> remove from list (remove from channel group) -> cycle comeplete (delete messages)
                case "request":
                    //Instantiate friend if they are a valid user.
                    FriendsListItem friendItem = Instantiate(friendsListItemPrefab, friendsListItemHandler);
                    friendItem.name = result.Publisher; // set the name to be able to access later for updates.
                    friendItem.tradeButton.gameObject.SetActive(false);
                    friendItem.acceptButton.gameObject.SetActive(true);
                    friendItem.removeButton.name = "reject"; // Used to determine whether or not to remove from friend groups
                    friendItem.gameObject.GetComponent<Image>().color = Color.yellow; // change color to show pending friend.
                    friendItem.Set(result.Publisher, PNManager.pubnubInstance.CachedPlayers[result.Publisher].Name);
                    
                    break;
                //Another user has accepted your friend request. Unblock buttons.
                case "accept":
                    FriendsListItem acceptFriend = GetFriend(result.Publisher);
                    if(acceptFriend != null)
                    {
                        acceptFriend.tradeButton.gameObject.SetActive(true);
                        acceptFriend.acceptButton.gameObject.SetActive(false);
                        acceptFriend.removeButton.name = "remove"; // Used to determine whether or not to remove from friend groups
                        acceptFriend.gameObject.GetComponent<Image>().color = Color.white; // change color to show accepted friend.
                    }

                    // Wipe Message History, as the friend request cycle has finished.
                    await PNManager.pubnubInstance.DeleteMessages(result.Channel);

                    break;
                //Another user has rejected or removed your friend request. Remove them from channel group. Rejecting and removing do same thing.
                case "reject":
                    FriendsListItem removeFriend = GetFriend(result.Publisher);
                    if (removeFriend != null)
                    {
                        await removeFriend.OnRemoveClick();                        
                    }

                    // Wipe Message History, as the friend request cycle has finished.
                    await PNManager.pubnubInstance.DeleteMessages(result.Channel);

                    break;             
                default:
                    Debug.Log("Not a valid friend request option.");
                    break;                
            }
        }
    }

    /// <summary>
    /// Listen for status updates to update online status of friends
    /// </summary>
    /// <param name="result"></param>
    private void OnPnPresence(PNPresenceEventResult result)
    {
        //Update to use channel group catching, rather than global
        if(result != null && result.Subscription != null && result.Subscription.Equals(PubNubUtilities.chanFriendChanGroupStatus + Connector.instance.GetPubNubObject().GetCurrentUserId()))
        {
            FriendsListItem friend = GetFriend(result.Uuid);
            if(friend != null)
            {
                if (result.Event.Equals("join"))
                {
                    friend.onlineStatus.color = Color.green;
                }

                else if (result.Event.Equals("leave") || result.Event.Equals("timeout"))
                {
                    friend.onlineStatus.color = Color.gray;
                }
            }           
        }
    }

    /// <summary>
    /// Listen for user event changes
    /// </summary>
    /// <param name="result"></param>
    private void OnPnObject(PNObjectEventResult result)
    {
        if(result != null)
        {
            // update existing player name
            if (result.Type.Equals("uuid") && result.Event.Equals("set"))
            {
                FriendsListItem friend = GetFriend(result.UuidMetadata.Uuid);
                if (friend != null)
                {
                    friend.nameText.text = result.UuidMetadata.Name;
                }
            }         
        }
    }

    /// <summary>
    /// Finds and returns the FriendListItem in the Friend's List
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private FriendsListItem GetFriend(string id)
    {
        Transform child = friendsListItemHandler.Find(id);

        if (child != null && child.TryGetComponent<FriendsListItem>(out var friendItem))
        {                       
            return friendItem;                 
        }
        return null;
    }

    /// <summary>
    /// Listen for Player Select Events
    /// </summary>
    public async void AddFriend(string action, string id)
    {
        //Close the popup if active
        if(privateMessagePopupPanel.activeSelf)
        {
            privateMessagePopupPanel.SetActive(false);
        }

        // Only focus on friend id additions. Ignore events coming in from other sources.
        if (action.Equals("selected") && gameObject.activeSelf && PNManager.pubnubInstance.CachedPlayers.ContainsKey(id))
        {
            FriendsListItem friendItem = Instantiate(friendsListItemPrefab, friendsListItemHandler);
            friendItem.name = id; // set the name to be able to access later for updates.
            friendItem.removeButton.name = "remove";
            //If a profile image or other metadata wants to be displayed for each player in the list, can update this function in the future.
            friendItem.Set(id, PNManager.pubnubInstance.CachedPlayers[id].Name);
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupStatus + Connector.instance.GetPubNubObject().GetCurrentUserId(), new string[] { PubNubUtilities.chanPresence + id });
            // Add friend to status feed group
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupChat + Connector.instance.GetPubNubObject().GetCurrentUserId(), new string[] { PubNubUtilities.chanFriendChat + id });
            string message = "request"; //initiates a friend request.
            // Send Message to indicate request has been made.
            PNResult<PNPublishResult> publishResponse = await Connector.instance.GetPubNubObject().Publish()
               .Channel(PubNubUtilities.chanFriendRequest + id) //chanFriendRequest channel reserved for handling friend requests.
               .Message(message)
               .ExecuteAsync();
            if(publishResponse.Status.Error)
            {
                Debug.Log("Error when sending message");
            }

            // Update friends list to get online status of new friend.
            await GetCurrentFriendOnlineStatus();
        }
    }

    /// <summary>
    /// Gets the list of friends associated with the friend group and populates the friend list.
    /// </summary>
    /// <param name="channelGroup"></param>
    /// <returns></returns>
    public async Task<bool> PopulateFriendList(string channelGroup)
    {    
        PNResult<PNChannelGroupsAllChannelsResult> cgListChResponse = await Connector.instance.GetPubNubObject().ListChannelsForChannelGroup()
            .ChannelGroup(channelGroup)
            .ExecuteAsync();

        if(cgListChResponse.Status != null && cgListChResponse.Status.Error)
        {
            return false;
        }

        if (cgListChResponse != null && cgListChResponse.Result != null
            && cgListChResponse.Result.Channels != null && cgListChResponse.Result.Channels.Count > 0)
        {                     
            foreach(var channel in cgListChResponse.Result.Channels)
            {
                //UserIds are contained within the channel names.
                string id = channel.Substring(PubNubUtilities.chanPresence.Length);
                //Don't add self to the friend list.
                if(!id.Equals(Connector.instance.GetPubNubObject().GetCurrentUserId()) && PNManager.pubnubInstance.CachedPlayers.ContainsKey(id))
                {
                    FriendsListItem friendItem = Instantiate(friendsListItemPrefab, friendsListItemHandler);
                    friendItem.name = id; // set the name to be able to access later for updates.
                    friendItem.removeButton.name = "remove";
                    friendItem.Set(id, PNManager.pubnubInstance.CachedPlayers[id].Name);
                }                  
            }

            //Obtain history of messages from the friend request channel to determine if there are any pending invites.
            PNResult<PNFetchHistoryResult> fetchHistoryResponse = await Connector.instance.GetPubNubObject().FetchHistory()
                .Channels(new string[] { PubNubUtilities.chanFriendRequest + Connector.instance.GetPubNubObject().GetCurrentUserId() })
                .ExecuteAsync();
           if(fetchHistoryResponse != null && fetchHistoryResponse.Result != null && fetchHistoryResponse.Result.Messages != null && !fetchHistoryResponse.Status.Error)
            {
                foreach (KeyValuePair<string, List<PNHistoryItemResult>> channel in fetchHistoryResponse.Result.Messages)
                {
                    foreach (PNHistoryItemResult item in channel.Value)
                    {
                        string entryAsString = (string)item.Entry;

                        // If a message with "request" is found, determine if it exists in the Friend List.
                        if (entryAsString.Equals("request"))
                        {
                            FriendsListItem potentialPendingFriend = GetFriend(item.Uuid);

                            //If so, ignore. Already friends.
                            //If not, that means the friend request is still pending. Find friend in list, mark as pending.
                            if (potentialPendingFriend == null)
                            {
                                potentialPendingFriend = Instantiate(friendsListItemPrefab, friendsListItemHandler);
                                potentialPendingFriend.name = item.Uuid; // set the name to be able to access later for updates.
                                potentialPendingFriend.tradeButton.gameObject.SetActive(false);
                                potentialPendingFriend.acceptButton.gameObject.SetActive(true);
                                potentialPendingFriend.removeButton.name = "reject"; // Used to determine whether or not to remove from friend groups
                                potentialPendingFriend.gameObject.GetComponent<Image>().color = Color.yellow; // change color to show pending friend.
                                potentialPendingFriend.Set(item.Uuid, PNManager.pubnubInstance.CachedPlayers[item.Uuid].Name);
                            }
                        }
                    }
                }
            }                      
            return true;          
        }

        //User first time logging in or hasn't opened the friend list. Add friends.
        else
        {
            // remember, channels wont be added again to channel groups if they've already been done so.
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupStatus + Connector.instance.GetPubNubObject().GetCurrentUserId(), new string[] { PubNubUtilities.chanPresence + Connector.instance.GetPubNubObject().GetCurrentUserId() });

            //Add self mpresene channel to status feed channel group
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupChat + Connector.instance.GetPubNubObject().GetCurrentUserId(), new string[] { PubNubUtilities.chanFriendChat + Connector.instance.GetPubNubObject().GetCurrentUserId() });
            return true;
        }
    }

    /// <summary>
    /// Determines the current online status of friends and updates their status.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> GetCurrentFriendOnlineStatus()
    {
        PNResult<PNHereNowResult> herenowResponse = await Connector.instance.GetPubNubObject().HereNow()
            .Channels(new string[]
            {
            PubNubUtilities.chanGlobal
            })
            .IncludeUUIDs(true)
            .ExecuteAsync();

        PNHereNowResult hereNowResult = herenowResponse.Result;
        PNStatus status = herenowResponse.Status;

        if (status != null && status.Error)
        {
            Debug.Log($"Error calling PubNub HereNow ({PubNubUtilities.GetCurrentMethodName()}): {status.ErrorData.Information}");
            return false;
        }

        else
        {
            foreach (KeyValuePair<string, PNHereNowChannelData> kvp in hereNowResult.Channels)
            {
                PNHereNowChannelData hereNowChannelData = kvp.Value as PNHereNowChannelData;
                if (kvp.Value != null)
                {
                    List<PNHereNowOccupantData> hereNowOccupantData = hereNowChannelData.Occupants as List<PNHereNowOccupantData>;
                    if (hereNowOccupantData != null)
                    {
                        foreach (PNHereNowOccupantData pnHereNowOccupantData in hereNowOccupantData)
                        {
                            //Go through and update online status per friend.
                            FriendsListItem friend = GetFriend(pnHereNowOccupantData.Uuid);
                            if (friend != null)
                            {                               
                                friend.onlineStatus.color = Color.green;
                            }
                        }
                    }
                }
            }
            return true;
        }
    } 
}
