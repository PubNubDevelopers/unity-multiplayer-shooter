using PubnubApi;
using PubNubUnityShowcase;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.tvOS;
using UnityEngine.UI;
using Visyde;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class FriendsList : MonoBehaviour
{
    //UI Fields
    public Transform friendsListItemHandler;       // this is where the room item prefabs will be spawned
    public FriendsListItem friendsListItemPrefab;         // the room item prefab (represents a game session in the lobby list)
    //public Text listStatusText;             // displays the current status of the lobby browser (eg. "No games available", "Fetching game list...")
    public Button searchFriendsButton;
    public GameObject privateMessagePopupPanel;

    //Internals


    // Start is called before the first frame update
    void Start()
    {
        //Listeners
        Connector.instance.OnPlayerSelect += AddFriend;
        Connector.instance.onPubNubPresence += OnPnPresence;
        Connector.instance.onPubNubObject += OnPnObject;

        //Initial Friend List Load
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDestroy()
    {
        Connector.instance.OnPlayerSelect -= AddFriend;
        Connector.instance.onPubNubPresence -= OnPnPresence;
        Connector.instance.onPubNubObject -= OnPnObject;
    }

    /// <summary>
    /// Listen for status updates to update online status of friends
    /// </summary>
    /// <param name="result"></param>
    private void OnPnPresence(PNPresenceEventResult result)
    {
        //if a specific channel name
        if(result != null && result.Channel.Equals(PubNubUtilities.chanGlobal))
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
    /// Listen for user and membership event changes
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

            //Triggerred whenever channel membership is updated(client gets added / removed from another player's friend list)
            else if (result.Type.Equals("membership"))
            {
                //Another player has added client as a friend
                if (result.Event.Equals("set"))
                {
                    AddFriend("selected", result.UuidMetadata.Uuid);
                }
                //Another player has removed client as a friend.
                else if (result.Event.Equals("delete"))
                {
                    //Friend should not be null at this point, but make the check anyway
                    FriendsListItem friend = GetFriend(result.UuidMetadata.Uuid);
                    if (friend != null)
                    {
                        friend.OnRemoveClick();
                    }
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
    public void AddFriend(string action, string id)
    {
        //Close the popup if active
        if(privateMessagePopupPanel.activeSelf)
        {
            privateMessagePopupPanel.SetActive(false);
        }

        // Only focus on friend id additions. Ignore messages coming in from chat.
        if (action.Equals("selected") && gameObject.activeSelf && PNManager.pubnubInstance.CachedPlayers.ContainsKey(id))
        {
            FriendsListItem friendItem = Instantiate(friendsListItemPrefab, friendsListItemHandler);
            friendItem.name = id; // set the name to be able to access later for updates.
            //If a profile image or other metadata wants to be displayed for each player in the list, can update this function in the future.
            friendItem.Set(id, PNManager.pubnubInstance.CachedPlayers[id].Name);

            //TODO: Add Friend to friend group
        }
    }


}
