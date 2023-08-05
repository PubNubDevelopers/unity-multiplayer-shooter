using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visyde;

public class FriendsList : MonoBehaviour
{
    //UI Fields
    public Transform friendsListItemHandler;       // this is where the room item prefabs will be spawned
    public FriendsListItem friendsListItemPrefab;         // the room item prefab (represents a game session in the lobby list)
    //public Text listStatusText;             // displays the current status of the lobby browser (eg. "No games available", "Fetching game list...")
    public Button searchFriendsButton;
    public GameObject privateMessagePopupPanel;

    // Start is called before the first frame update
    void Start()
    {
        Connector.instance.OnPlayerSelect += AddFriend; 
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDestroy()
    {
        Connector.instance.OnPlayerSelect -= AddFriend;
    }

    /// <summary>
    /// Listen for Player Select Events
    /// </summary>
    public void AddFriend(string action, string id)
    {
        //Close the popup
        privateMessagePopupPanel.SetActive(false);

        // Only focus on friend id additions. Ignore messages coming in from chat.
        if (action.Equals("selected") && gameObject.activeSelf && PNManager.pubnubInstance.CachedPlayers.ContainsKey(id))
        {
            FriendsListItem friendItem = Instantiate(friendsListItemPrefab, friendsListItemHandler);
            //If a profile image or other metadata wants to be displayed for each player in the list, can update this function in the future.
            friendItem.Set(id, PNManager.pubnubInstance.CachedPlayers[id].Name);

            //TODO: Add Friend to Friend Group
        }
    }
}
