using System.Collections;
using System.Collections.Generic;
using PubnubApi;
using UnityEngine;
using UnityEngine.UI;

public class FilterPlayers : MonoBehaviour
{
    //UI Fields
    public Button listItemPrefab; 
    public Transform contentPanel; 
    public InputField searchPlayersInput;
    //public Dropdown dropdown;

    //Internals

    //Controls how many private message recipients you would like to store.
    private int numPrivateMessageRecipients = 3;


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
        PNManager.pubnubInstance.PrivateMessageUUID = id;
        //Use the dropdown from Chat.cs to trigger event handlers.
        Dropdown chatDropdown = GameObject.Find("ChatTargetDropdown").GetComponent<Dropdown>();
        if(chatDropdown != null)
        {
            //Create a new dropdown option for the communication with the private player. Use their nickname.
            Dropdown.OptionData privateMessageOption = new Dropdown.OptionData(PNManager.pubnubInstance.CachedPlayers[PNManager.pubnubInstance.PrivateMessageUUID].Name);

            //Allow only 4 options: ALL, Friends, Whisper, and Private Message with a specific person.
            //This overwrites the previous specific option if was previously set.
            if(chatDropdown.options.Count > 3)
            {
                chatDropdown.options[3] = privateMessageOption;
            }

            else
            {
                chatDropdown.options.Add(privateMessageOption);
            }

            //Change the target to trigger the on change event in Chat.cs.
            chatDropdown.value = chatDropdown.options.Count - 1;
        }       
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
