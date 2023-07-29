using System.Collections;
using System.Collections.Generic;
using PubnubApi;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class FilterPlayers : MonoBehaviour
{
    //UI Fields
    public Button listItemPrefab; 
    public Transform contentPanel; 
    public InputField searchPlayersInput;

    void Start()
    {
        //In case the user closes the popup window and re-opens
        if(contentPanel.childCount > 0)
        {
            ClearPlayerSearchList();
        }

        PopulateList();
    }

    /// <summary>
    /// Populates the list of players.
    /// </summary>
    void PopulateList()
    {
        foreach (KeyValuePair<string, UserMetadata> cachedPlayer in PNManager.pubnubInstance.CachedPlayers)
        {
            CreatePlayerItem(cachedPlayer.Value.Uuid, cachedPlayer.Value.Name);
        }
    }

    /// <summary>
    /// Called when the user clicks on a player
    /// </summary>
    /// <param name="id">The UserID of the clicked player</param>
    public void OnPlayerClick(string id)
    {  
        //The UUID is used to track for sending private messages as Names are not unique.
        PNManager.pubnubInstance.PrivateMessageUUID = id;
        //Use the dropdown from Chat.cs to trigger event handlers.
        Connector.instance.DropdownChange(true, PNManager.pubnubInstance.CachedPlayers[PNManager.pubnubInstance.PrivateMessageUUID].Name);
    }
    
    /// <summary>
    /// Gets called anytime the user is attempting to filter for players using an onchangeevent.
    /// Once users start typing, trigger onchangedevent for the nameinput
    /// </summary>
    public void OnPlayerSearchChange()
    {
        //Once event triggers, as user starts typing, clear all other users.
        ClearPlayerSearchList();

        //If completely clear search, bring back first 20 users.
        if (string.IsNullOrWhiteSpace(searchPlayersInput.text))
        {
            PopulateList();
        }

        else
        {
            //Filter every cached player by name. Create gameobject for each of these players.
            foreach (KeyValuePair<string, UserMetadata> cachedPlayer in PNManager.pubnubInstance.CachedPlayers)
            {
                //If users name hit a match, then add to list.
                //Don't add own user to the list.
                if (cachedPlayer.Value.Name.ToLowerInvariant().StartsWith(searchPlayersInput.text.ToLowerInvariant())
                    && !cachedPlayer.Value.Uuid.Equals(PNManager.pubnubInstance.pubnub.GetCurrentUserId())) //lower case the text to allow for case insensitivity
                {
                    CreatePlayerItem(cachedPlayer.Value.Uuid, cachedPlayer.Value.Name);
                }
            }    
        }
    }

    /// <summary>
    /// Creates the row that contains each player to select from.
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="name"></param>
    private void CreatePlayerItem(string uuid, string name)
    {
        Button newItem = Instantiate(listItemPrefab, contentPanel);
        newItem.name = uuid; //the name is used to extract the user id later on.
        Text buttonText = newItem.GetComponentInChildren<Text>();
        buttonText.text = name;
        newItem.onClick.AddListener(() => OnPlayerClick(uuid));
    }

    /// <summary>
    /// Clears the list of any players in the content panel.
    /// </summary>
    private void ClearPlayerSearchList()
    {
        foreach(Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }
}
