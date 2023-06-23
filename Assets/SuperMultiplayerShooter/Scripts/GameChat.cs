using System;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using PubNubAPI;
using UnityEngine;
using UnityEngine.UI;

public class GameChat : MonoBehaviour
{
    [Header("Settings:")]
    public Color ourColor;
    public Color othersColor;
    public Color ourChatColor;
    public Color othersChatColor;
    public Color positiveNotifColor;
    public Color negativeNotifColor;
    [Header("References:")]
    public Text messageDisplay;
    public InputField inputField;

    //Internals
    private string _gameSubscribe = "chat.game."; //Wildcard subscribe to listen for all channels
    private string _gamePublish = "chat.game."; //Publish channel will include
    private PubNub _pubnub;

    // Start is called before the first frame update
    void Start()
    {
        _pubnub = PubNubManager.Instance.InitializePubNub();

        //Update channel names to reflect the current room.
        _gameSubscribe += PhotonNetwork.CurrentRoom.Name; //Guarenteed to be unique, set by server.
        _gamePublish += PhotonNetwork.CurrentRoom.Name;
        messageDisplay.text = "";

        //Listen for any new incoming messages
        _pubnub.SubscribeCallback += (sender, e) => {
            SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            if (mea.MessageResult != null)
            {
                string message = ParseMessage(mea.MessageResult.Payload);
                GetUsername(mea.MessageResult, message);
            }
        };

        //Subscribe to the lobby chat channel
        _pubnub.Subscribe()
           .Channels(new List<string>(){
                    _gameSubscribe
           })
           .Execute();
    }

    // Update is called once per frame
    void Update()
    {
        //Can always check to see if pubnub connection still active.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendChatMessage();
            inputField.ActivateInputField(); //set focus back to chat input field after sending message
        }
    }

    /// <summary>
    /// Called when the scene ends or the game closes. Unsubscribe from game channel.
    /// </summary>
    private void OnDestroy()
    {
        //Unsubscrube from lobby chat once player leaves the room.
        _pubnub.Unsubscribe()
            .Channels(new List<string>()
            {
                _gameSubscribe
            })
            .Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log(string.Format("Unsubscribe Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                }
                else
                {
                    Debug.Log(string.Format("DateTime {0}, In Unsubscribe, result: {1}", DateTime.UtcNow, result.Message));
                }
            });
    }

    /// <summary>
    /// Publishes the chat message.
    /// </summary>
    public void SendChatMessage()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            MessageModeration filter = new MessageModeration();
            filter.text = inputField.text;

            _pubnub.Publish()
                .Channel(_gamePublish)
                .Message(filter)
                .Async((result, status) => {
                    if (status.Error)
                    {
                        Debug.Log(status.Error);
                        Debug.Log(status.ErrorData.Info);
                    }
                });
            //clear input field.
            inputField.text = string.Empty;
        }
    }

    /// <summary>
    /// Displays the chat mesage. If the message contains profanity, simply prints the whole message as asterisks (*)
    /// </summary>
    /// <param name="message">The message from the user</param>
    /// <param name="from">The user who the sent the message</param>
    /// <param name="ours">Is the message sent from client</param>
    /// <param name="systemMessage">Message sent by systen.</param>
    /// <param name="negative">Color scheme</param>
    void DisplayChat(string message, string from, bool ours, bool systemMessage, bool negative)
    {
        string finalMessage = "";

        if (systemMessage)
        {
            finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(negative ? negativeNotifColor : positiveNotifColor) + ">" + message + "</color>";
        }
        else
        {
            finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>: <color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
        }
        messageDisplay.text += finalMessage;

        // Canvas refresh:
        Canvas.ForceUpdateCanvases();    
    }

    /// <summary>
    /// Obtains the username and displays the chat.
    /// </summary>
    /// <param name="message"></param>
    private void GetUsername(PNMessageResult payload, string message)
    {
        string username = "";
        if (PubNubManager.Instance.CachedPlayers.ContainsKey(payload.IssuingClientId))
        {
            username = PubNubManager.Instance.CachedPlayers[payload.IssuingClientId].Name;
        }

        //Check in case the username is null. Set to back-up of UUID just in case.
        if (string.IsNullOrWhiteSpace(username))
        {
            username = payload.IssuingClientId;
        }

        //Display the chat pulled.
        DisplayChat(message, username, PubNubManager.Instance.UserId.Equals(payload.IssuingClientId), false, false);
    }

    /// <summary>
    /// Parses the payload for messages containing containing both non-profanity/profanity messages.
    /// If the payload does contain profanity, simply prints the entire message as asterisks.
    /// You can, however, isolate the profanity and replace only the words containing profanity with asterisks.
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    private string ParseMessage(object payload)
    {
        object message = "";
        var metaDataJSON = JsonConvert.SerializeObject(payload);

        //Extracting text from the payload. If it cannot be found, simply print a temp message.
        var messageDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaDataJSON);
        if (messageDictionary == null || !messageDictionary.TryGetValue("text", out message))
        {
            message = "<...>";
        }
        return message.ToString();
    }
}