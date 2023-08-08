using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using UnityEngine.EventSystems;
using Visyde;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.Localization.Settings;
using Unity.VisualScripting;

public class Chat : MonoBehaviour
{
    //UI Fields
    [Header("Settings:")]
    public Color allChatColor;
    public Color friendsChatColor;
    public Color privateChatColor;
    public Color lobbyChatColor;
    public Color systemNotificationColor;

    [Header("UI References")]
    public Dropdown chatTargetDropdown;
    public Text messageDisplay;
    public InputField inputField;
    public GameObject privateMessagePopupPanel;
    public GameObject chatView;
    public Button closeChatButton;
    public Text chatOpenText;
    public Button chatButton;
    public EventSystem eventSystem;
    public GameObject friendsListPanel;

    //Internals
    private string targetChatChannel = PubNubUtilities.chanChatAll; // intended target when sending chat messages

    // Start is called before the first frame update
    void Start()
    {
        //Set focus to the input field.
        eventSystem.SetSelectedGameObject(inputField.gameObject);

        //Add Listeners
        Connector.instance.onPubNubMessage += OnPnMessage;
        chatTargetDropdown.onValueChanged.AddListener(delegate
        {
            ChatTargetChanged(chatTargetDropdown);
        });

        //Subscribe to trigger events whenever a new dropdown option is added.
        Connector.instance.OnPlayerSelect += UpdateDropdown;
    }

    // Update is called once per frame
    void Update()
    {
        //Closes the private message panel if the user clicks escape.
        if(privateMessagePopupPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            ClosePrivateMessagePopup();
        }

        //Force cursor the blink in input field.
        if(inputField.gameObject.activeSelf && string.IsNullOrWhiteSpace(inputField.text))
        {
            StartCoroutine(EnableInputField());
        }

        //Handle when the user presses the enter key.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if(inputField.gameObject.activeSelf)
            {
                //If there is text to send, send it.
                if (!string.IsNullOrWhiteSpace(inputField.text))
                {
                    //Include the selected locale language whenever you are sending messages.
                    //For sending messages, it doesn't matter what the languages are in, provided you include the source language.
                    MessageModeration translateMessage = new MessageModeration();
                    translateMessage.text = inputField.text;
                    translateMessage.source = LocalizationSettings.SelectedLocale.Identifier.Code;
                    translateMessage.target = LocalizationSettings.SelectedLocale.Identifier.Code;
                    translateMessage.publisher = Connector.instance.GetPubNubObject().GetCurrentUserId();
                    //Determine color of message based on channel. Default to all chat.
                    Color color = allChatColor;

                    //Private Chat
                    if (targetChatChannel.StartsWith(PubNubUtilities.chanPrivateChat[..^1]))
                    {
                        color = privateChatColor;
                    }

                    //Friends
                    else if (targetChatChannel.StartsWith(PubNubUtilities.chanFriendChat))
                    {
                        color = friendsChatColor;
                    }

                    //Lobby
                    else if (targetChatChannel.StartsWith(PubNubUtilities.chanChatLobby))
                    {
                        if (Connector.instance.CurrentRoom != null && targetChatChannel.Equals(PubNubUtilities.chanChatLobby + Connector.instance.CurrentRoom.ID))
                        {
                            color = lobbyChatColor;
                        }
                    }

                    // Display your nickname as the recipient.
                    DisplayChat(translateMessage.text, Connector.PNNickName, color);

                    SendChatMessage(translateMessage, targetChatChannel);
                }

                //Otherwise close the chat window.
                else
                {
                    CloseChatWindow();
                }
            }

            //Open the chat window
            else
            {
                OpenChatWindow();
            }         
        }
    }

    //This function forces the to cursor to blink while the input field is active.
    //This fixes the problem caused when opening and closing the chat multiple times and is fixed by multiple "active" calls.
    //AI Generated by ChatGPT
    public IEnumerator EnableInputField()
    {
        inputField.gameObject.SetActive(true);
        yield return null; // wait until next frame
        eventSystem.SetSelectedGameObject(inputField.gameObject, null);
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.button = PointerEventData.InputButton.Left;
        inputField.OnPointerClick(eventData); // Simulate first click
        yield return null; // wait until next frame
        inputField.OnPointerClick(eventData); // Simulate second click
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    /// <summary>
    /// Handle the change event when the user selects an option in the drop-down list.
    /// Used to determine the intended publish chat target.
    /// </summary>
    /// <param name="dropdown"></param>
    void ChatTargetChanged(Dropdown dropdown)
    {
        dropdown.RefreshShownValue();
        //Check in case the popup panels are still active. Close if they are
        if(privateMessagePopupPanel.activeSelf)
        {
            ClosePrivateMessagePopup();
        }

        //If chat window is not open, force it open.
        if (!chatTargetDropdown.gameObject.activeSelf)
        {
            OpenChatWindow();
        }

        string selectedText = dropdown.options[dropdown.value].text;
        switch(selectedText)
        {
            case "All":
                Debug.Log("You selected All");
                targetChatChannel = PubNubUtilities.chanChatAll;
                // Include the logic you want to happen when "ALL" is selected
                break;
            case "Private":
                Debug.Log("You selected Private");
                targetChatChannel = PubNubUtilities.chanPrivateChat;

                // Close Friend List panel to not interfere with sending private message.
                if(friendsListPanel.activeSelf)
                {
                    friendsListPanel.SetActive(false);
                }

                //Opens the panels.          
                privateMessagePopupPanel.SetActive(true);

                //Disable input field to not take priority over searching for other players.
                inputField.gameObject.SetActive(false);
                break;
            case "Friends":
                targetChatChannel = PubNubUtilities.chanFriendChat + Connector.instance.GetPubNubObject().GetCurrentUserId();
                Debug.Log("You selected Friends");
                // Include the logic you want to happen when "FRIENDS" is selected
                break;
            case "Lobby":
                Debug.Log("You selected Lobby");
                if(Connector.instance.CurrentRoom != null)
                {
                    targetChatChannel = PubNubUtilities.chanChatLobby + Connector.instance.CurrentRoom.ID;
                }
                //Failsafe to go back to all chat in case the lobby room does not exist.
                else
                {
                    targetChatChannel = PubNubUtilities.chanChatAll;
                }
                break;
            default:
                // Reserved for private messages.
                //Failsafe in case for whatever reason the option is empty.
                if(!string.IsNullOrWhiteSpace(selectedText))
                {
                    // Create the private channel and set the targetChatChannel appropriately.
                    // publising a private message will always start with the targer user's uuid.
                    // subscribe will always be in the format starting with your uuid.
                    targetChatChannel = $"chat.private.{PNManager.pubnubInstance.PrivateMessageUUID}&{Connector.instance.GetPubNubObject().GetCurrentUserId()}";
                }

                else
                {
                    targetChatChannel = PubNubUtilities.chanChatAll;
                }
                break;
        }
    }

    /// <summary>
    /// Called whenever the scene or game ends. Unsbscribe from event listeners.
    /// </summary>
    private void OnDestroy()
    {
        Connector.instance.onPubNubMessage -= OnPnMessage;
        Connector.instance.OnPlayerSelect -= UpdateDropdown;
    }

    /// <summary>
    /// Closes the Private Message Popup (user search) and the darken panel when the player has either selected
    /// another another player to whisper or closes the player search.
    /// </summary>
    public void ClosePrivateMessagePopup()
    {
        privateMessagePopupPanel.SetActive(false);

        //Don't force inputfield active if looking to add friends via Friends List.
        if(!friendsListPanel.activeSelf)
        {
            inputField.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Event listener to handle PubNub Message events
    /// </summary>
    /// <param name="pn"></param>
    /// <param name="result"></param>
    private void OnPnMessage(PNMessageResult<object> result)
    {
        //all chat messages start with "chat" or "translate". Ignore messages from self (they are not translated/ran through profanity filter).
        if (result != null && !string.IsNullOrWhiteSpace(result.Message.ToString())
            && !string.IsNullOrWhiteSpace(result.Channel) 
            && ((result.Channel.StartsWith(PubNubUtilities.chanChat) && !result.Publisher.Equals(Connector.instance.GetPubNubObject().GetCurrentUserId()))
            || result.Channel.StartsWith(PubNubUtilities.chanChatTranslate)))
        {
            Color color = new Color(0, 0, 0, 0);
            string channel = result.Channel;
            //Replace channel name with chat. to determine the color of the chat.
            if(result.Channel.StartsWith(PubNubUtilities.chanChatTranslate))
            {
                channel = PubNubUtilities.chanChat + result.Channel.Substring(PubNubUtilities.chanChatTranslate.Length);
            }
            //Private Chat
            if (channel.StartsWith(PubNubUtilities.chanPrivateChat[..^1]))
            {
                if (channel.Contains(Connector.instance.GetPubNubObject().GetCurrentUserId()))
                {
                    color = privateChatColor;
                }

                //Automatically add an option to the dropdown from the recipient if a private message
                //option has not been added before. Don't actually switch dropdown selection,
                //simply provide as an option.
               if(string.IsNullOrEmpty(PNManager.pubnubInstance.PrivateMessageUUID))
               {
                    string displayName = PNManager.pubnubInstance.CachedPlayers[result.Publisher].Name;
                    Dropdown.OptionData newOption = new Dropdown.OptionData(displayName);
                    PNManager.pubnubInstance.PrivateMessageUUID = result.Publisher;
                    chatTargetDropdown.options.Add(newOption);
               }
            }
            
            //Friends
            else if (channel.StartsWith(PubNubUtilities.chanFriendChat))
            {
                color = friendsChatColor;
            }
            
            //Lobby
            else if(channel.StartsWith(PubNubUtilities.chanChatLobby)) 
            {
                if (Connector.instance.CurrentRoom != null && channel.Equals(PubNubUtilities.chanChatLobby + Connector.instance.CurrentRoom.ID))
                {
                    color = lobbyChatColor;

                }
            }

            //All Chat
            else
            {
                color = allChatColor;
            }

            //If color wasn't set, then it means the message isn't meant for us to display.
            if(color != default(Color))
            {
                try
                {                  
                    string message = "";

                    //Message is already filtered for profanity at this point.
                    //Determine if we need to translate the language before we display the chat.
                    var moderatedMessage = JsonConvert.DeserializeObject<MessageModeration>(result.Message.ToString());
                    if(moderatedMessage != null)
                    {
                        message = moderatedMessage.text;

                        //If the languages match and  (don't want to display chat twice), it's already been translated or is
                        //already in the same language, no need to do any more work. Just display the chat.
                        if (LocalizationSettings.SelectedLocale.Identifier.Code.Equals(moderatedMessage.source))
                        {
                            string username = GetUsername(moderatedMessage.publisher);
                            //Did not come from translate, display it.
                            if (result.Channel.StartsWith(PubNubUtilities.chanChat) 
                                || (result.Channel.StartsWith(PubNubUtilities.chanChatTranslate) && !moderatedMessage.publisher.Equals(Connector.instance.GetPubNubObject().GetCurrentUserId())))
                            {
                                DisplayChat(message, username, color);
                            }
                        }

                        //If they don't match, translate language. Ignore messages coming from translate language, to avoid duplicating message.
                        else if(!result.Channel.StartsWith(PubNubUtilities.chanChatTranslate))
                        {
                            //Prep the channel name to send to the PubNub Function. Replace "chat" with "translate" as we already know it's a chat message.
                            string translateChannel = PubNubUtilities.chanChatTranslate + channel.Substring(PubNubUtilities.chanChat.Length);

                            //Include the selected locale language whenever you are sending messages.
                            MessageModeration translateMessage = new MessageModeration();
                            translateMessage.text = message;
                            translateMessage.source = moderatedMessage.source;
                            translateMessage.target = LocalizationSettings.SelectedLocale.Identifier.Code;
                            translateMessage.publisher = result.Publisher; //used to display the original name of hte user who published message.
                            SendChatMessage(translateMessage, translateChannel);
                        }                      
                    }                 
                }

                catch(Exception e)
                {
                    Debug.Log($"Error when attempting to extract information from message: {e.Message}");
                }                    
            }     
        }
    }

    /// <summary>
    /// Publishes the chat message.
    /// </summary>
    public async void SendChatMessage(MessageModeration message, string channel)
    {
        //clear input field.
        inputField.text = string.Empty;

        PNResult<PNPublishResult> publishResponse = await Connector.instance.GetPubNubObject().Publish()
            .Channel(channel)
            .Message(message)
            .ExecuteAsync();     
        
        if (publishResponse != null && publishResponse.Status != null && publishResponse.Status.Error)
        {
            Debug.Log($"Error when attempting to send publish message: {publishResponse.Status.ErrorData}");
        }
    }

    /// <summary>
    /// Displays the chat mesage
    /// </summary>
    /// <param name="message">The message from the user</param>
    /// <param name="recipient">The user who the sent the message</param>
    /// <param name="color">The color of the chat, representing whom it came from</param>
    void DisplayChat(string message, string recipient, Color color)
    {
        //Forat color to be read in an HTML string.
        string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(color);
        string finalMessage = $"<color=#{colorHex}>{recipient}:{message}</color>\n";
        messageDisplay.text += finalMessage;
    }

    /// <summary>
    /// Obtains the username and displays the chat.
    /// </summary>
    /// <param name="message"></param>
    private string GetUsername(string uuid)
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

    /// <summary>
    /// "Opens" the chat window. Enables all other hidden gameobjects and changes the alpha values of previously hidden windows.
    /// </summary>
    public void OpenChatWindow()
    {
        //Restore hidden game objects.
        chatTargetDropdown.gameObject.SetActive(true);
        inputField.gameObject.SetActive(true);
        closeChatButton.gameObject.SetActive(true);
        chatOpenText.gameObject.SetActive(true);
        chatButton.gameObject.SetActive(false);

        //Change the Alpha values of the Chat and ChatView windows back to 1 (no longer transparent).
        Image chatImage = gameObject.GetComponent<Image>();
        ChangeAlphaValue(chatImage, 1f);

        Image chatViewImage = chatView.GetComponent<Image>();
        ChangeAlphaValue(chatViewImage, 0.75f);
    }


    /// <summary>
    /// "Closes" the chat window. Still displays the chat when coming in, but hides other gameobjects until the user "opens" the window.
    /// </summary>
    public void CloseChatWindow()
    {
        //Whenever the chat window is closed, display chat in reduced form.
        chatTargetDropdown.gameObject.SetActive(false);
        inputField.gameObject.SetActive(false);
        closeChatButton.gameObject.SetActive(false);
        chatOpenText.gameObject.SetActive(false);
        chatButton.gameObject.SetActive(true);

        //Change the Alpha values of the Chat and ChatView windows to 0.
        Image chatImage = gameObject.GetComponent<Image>();
        ChangeAlphaValue(chatImage, 0f);
      
        Image chatViewImage = chatView.GetComponent<Image>();
        ChangeAlphaValue(chatViewImage, 0f);
    }

    /// <summary>
    /// Changes the transparancy give the value of the provided image
    /// </summary>
    /// <param name="image"></param>
    /// <param name="value"></param>
    private void ChangeAlphaValue(Image image, float value)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = value;
            image.color = color;
        }
    }

    /// <summary>
    /// Update the dropdown with the new option received.
    /// </summary>
    /// <param name="id"></param>
    public void UpdateDropdown(string action, string id)
    {
        // Immediately exit if the event is triggered when selecting friends.
        if(friendsListPanel.activeSelf && action.Equals("selected")) 
        {
            return;
        }

        string displayName = id;
        bool privateMessage = PNManager.pubnubInstance.CachedPlayers.ContainsKey(id);
        //Get the username of the ID which will represent the dropdown option.
        //If the key does not exist, then it means it is not a private message option.
        if (privateMessage)
        {
            displayName = PNManager.pubnubInstance.CachedPlayers[id].Name;
        }

        int index = chatTargetDropdown.options.FindIndex((options) => options.text == displayName);

        //Add the new option
        if (action.Equals("chat-add") || action.Equals("selected")) 
        {
            //If the option is from a private message, then check to see if the option already exists.
            //If it does, then overwrite the dropdown with the new private message player. Otherwise create it.
            //Option doesn't exist yet - create it.
            if (index < 0 && (id.Equals("Lobby") || (privateMessage && string.IsNullOrEmpty(PNManager.pubnubInstance.PrivateMessageUUID))))
            {
                Dropdown.OptionData newOption = new Dropdown.OptionData(displayName);
                chatTargetDropdown.options.Add(newOption);
                index = chatTargetDropdown.options.Count - 1;
            }

            //The option exists, change the private message target.
            else
            {
                if (!PNManager.pubnubInstance.PrivateMessageUUID.Equals("") &&
                    PNManager.pubnubInstance.CachedPlayers.ContainsKey(PNManager.pubnubInstance.PrivateMessageUUID))
                {
                    string previousPrivateMessageName = PNManager.pubnubInstance.CachedPlayers[PNManager.pubnubInstance.PrivateMessageUUID].Name;
                    index = chatTargetDropdown.options.FindIndex((options) => options.text == previousPrivateMessageName);
                    chatTargetDropdown.options[index].text = displayName;
                }
            }

            if (privateMessage)
            {
                PNManager.pubnubInstance.PrivateMessageUUID = id;
            }

            //Change the target to trigger the on change event in Chat.cs.
            chatTargetDropdown.value = index;     
        }

        //Remove the specific option
        else if(action.Equals("chat-remove"))
        {
            //Finds the index of the id in the dropdown list if it exists.
            if(chatTargetDropdown.options[index] != null)
            {
                chatTargetDropdown.options.RemoveAt(index);

                //Change to All chat.
                chatTargetDropdown.value = 0;
            }          
        }
    } 
}
