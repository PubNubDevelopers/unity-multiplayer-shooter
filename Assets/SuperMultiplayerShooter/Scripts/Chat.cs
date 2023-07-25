using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using UnityEngine.EventSystems;

public class Chat : MonoBehaviour
{
    //UI Fields
    //[Header("Chat Target Dropdown")]
    [Header("UI Fields")]
    public Dropdown chatTargetDropdown;
    public Text messageDisplay;
    public InputField inputField;
    public Button sendButton;
    public GameObject loadingIndicator;
    public GameObject privateMessagePopupPanel;
    public GameObject privateMessageDarkenPanel;
    //public Dropdown languageOptions;

    //Internals
    //private string _lobbySubscribe = "chat.translate."; //Wildcard subscribe to listen for all channels
    //private string _lobbyPublish = "chat.translate."; //Publish channel will include
    private string allChat = "chat.all";
    private string cgFriendList = "friends-";
    private string friendChat = "presence-";
    private string whisperChat = $"chat.private.*";
    private string targetChatChannel = ""; // intended target when sending chat messages
    private Pubnub pubnub;
    private SubscribeCallbackListener listener = new SubscribeCallbackListener();

    // Start is called before the first frame update
    void Start()
    {
        //Initializes the PubNub Connection.
        pubnub = PNManager.pubnubInstance;

        //Add Listeners
        pubnub.AddListener(listener);
        listener.onMessage += OnPnMessage;
        Debug.Log($"UUID:{pubnub.GetCurrentUserId()}");
        //whisperChat += $"{pubnub.GetCurrentUserId()}&*";
        targetChatChannel = allChat; // default to all channel
        friendChat += pubnub.GetCurrentUserId(); //Private channels in form of "presence-<UserId>". Used to publish so friend groups can be notified of messages.
        cgFriendList += pubnub.GetCurrentUserId(); //Manages the friend lists.


        chatTargetDropdown.onValueChanged.AddListener(delegate
        {
            ChatTargetChanged(chatTargetDropdown);
        });

        //Subscribe to the list of Channels and channel groups. Subscribe events happen elsewhere
        pubnub.Subscribe<string>()
           .Channels(new string[]
           {
               allChat,
               whisperChat
           })
           .ChannelGroups(new string[]
           {
               cgFriendList // listen for friend list
           })
           .Execute();

        //Close darken panel click events (to close private message search friends list)
        EventTrigger eventTrigger = privateMessageDarkenPanel.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((eventData) => {
            ClosePrivateMessagePopup();
        });
    }

    // Update is called once per frame
    void Update()
    {
        //Closes the private message panel if the user clicks escape.
        if(privateMessagePopupPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            ClosePrivateMessagePopup();
        }
        //Can always check to see if pubnub connection still active.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendChatMessage();
        }
    }

    /// <summary>
    /// Handle the change event when the user selects an option in the drop-down list.
    /// Used to determine the intended publish chat target.
    /// </summary>
    /// <param name="dropdown"></param>
    void ChatTargetChanged(Dropdown dropdown)
    {
        Debug.Log("Selected Value : " + dropdown.value); //This will log the index of the selected option
        //Check in case the popup panels are still active. Close if they are
        if(privateMessagePopupPanel.activeSelf || privateMessageDarkenPanel.activeSelf)
        {
            ClosePrivateMessagePopup();
        }
        string selectedText = dropdown.options[dropdown.value].text;
        switch(selectedText)
        {
            case "ALL":
                Debug.Log("You selected ALL");
                targetChatChannel = allChat;
                // Include the logic you want to happen when "ALL" is selected
                break;
            case "WHISPER":
                Debug.Log("You selected WHISPER");
                targetChatChannel = whisperChat;

                //Opens the panels.          
                privateMessagePopupPanel.SetActive(true);
                privateMessageDarkenPanel.SetActive(true);       
                break;
            case "FRIENDS":
                targetChatChannel = friendChat;
                Debug.Log("You selected FRIENDS");
                // Include the logic you want to happen when "FRIENDS" is selected
                break;
            default:
                // Reserved for private messages.
                //Failsafe in case for whatever reason the option is empty.
                if(!string.IsNullOrWhiteSpace(selectedText))
                {
                    // Create the private channel and set the targetChatChannel appropriately.
                    // publising a private message will always start with the targer user's uuid.
                    // subscribe will always be in the format starting with your uuid.
                    targetChatChannel = $"chat.private.{PNManager.pubnubInstance.PrivateMessageUUID}&{pubnub.GetCurrentUserId()}";
                }

                else
                {
                    targetChatChannel = allChat;
                }
                break;
        }
    }

    /// <summary>
    /// Called whenever the scene or game ends. Unsbscribe from event listeners.
    /// </summary>
    private void OnDestroy()
    {
        listener.onMessage -= OnPnMessage;
    }

    /// <summary>
    /// Closes the Private Message Popup (user search) and the darken panel when the player has either selected
    /// another another player to whisper or closes the player search.
    /// </summary>
    public void ClosePrivateMessagePopup()
    {
        privateMessageDarkenPanel.SetActive(false);
        privateMessagePopupPanel.SetActive(false);      
    }

    /// <summary>
    /// Event listener to handle PubNub Message events
    /// </summary>
    /// <param name="pn"></param>
    /// <param name="result"></param>
    private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
    {
        //TODO: dont include leaderboard channel msgs
        if (result != null && !string.IsNullOrWhiteSpace(result.Message.ToString())
            && !string.IsNullOrWhiteSpace(result.Channel) && !result.Channel.Equals("leaderboard_scores"))
        {
            string color = "";
            if (result.Channel.StartsWith(whisperChat[..^1]))
            {
                if (result.Channel.Contains(pubnub.GetCurrentUserId()))
                {
                    color = "green";
                }        
            }

            //Friends
            else if(result.Channel.StartsWith("presence"))
            {
                color = "orange";
            }

            else
            {
                color = "white";
            }

            //If color wasn't set, then it means the message isn't meant for us to display.
            if(!string.IsNullOrWhiteSpace(color))
            {
                string message = result.Message.ToString();
                string username = GetUsername(result.Publisher, message);
                DisplayChat(message, username, color);
            }     
        }
    }

    /// <summary>
    /// Publishes the chat message.
    /// </summary>
    public async void SendChatMessage()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        { 
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                .Channel(targetChatChannel)
                .Message(inputField.text)
                .ExecuteAsync();

            PNPublishResult publishResult = publishResponse.Result;
            PNStatus status = publishResponse.Status;

            //clear input field.
            inputField.text = string.Empty;
        }
    }

    /// <summary>
    /// Displays the chat mesage
    /// </summary>
    /// <param name="message">The message from the user</param>
    /// <param name="recipient">The user who the sent the message</param>
    /// <param name="color">The color of the chat, representing whom it came from</param>
    void DisplayChat(string message, string recipient,string color)
    { 
       string finalMessage = $"<color={color}>{recipient}:{message}</color>\n";
       messageDisplay.text += finalMessage;
    }

    /// <summary>
    /// Obtains the username and displays the chat.
    /// </summary>
    /// <param name="message"></param>
    private string GetUsername(string uuid, string message)
    {
        string username = "";
        if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(uuid))
        {
            username = PNManager.pubnubInstance.CachedPlayers[uuid].Name;
        }

        //Check in case the username is null. Set to back-up of UUID just in case.
        if (string.IsNullOrWhiteSpace(username))
        {
            username = uuid;
        }

        return username;
    }
}
