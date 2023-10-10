using Newtonsoft.Json;
using PubnubApi;
using PubNubUnityShowcase;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Visyde;
using static PubnubApi.Unity.PubnubExtensions;

public class Leaderboard : MonoBehaviour
{
    // leaderboard
    [Header("UI References")]
    public Text namePos1;
    public Text namePos2;
    public Text namePos3;
    public Text namePos4;
    public Text namePos5;
    public Text kdPos1;
    public Text kdPos2;
    public Text kdPos3;
    public Text kdPos4;
    public Text kdPos5;
    private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }

    // Start is called before the first frame update
    void Start()
    {
        PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;
        PNManager.pubnubInstance.onPubNubReady += OnPnReady;
    }

    /// <summary>
    /// Called whenever the scene or game ends. Unsubscribe event listeners.
    /// </summary>
    void OnDestroy()
    {
        PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
    }

    /// <summary>
    /// Waits until the connector is ready before attempting to publish using pubnub object.
    /// </summary>
    private async void OnPnReady()
    {
        //fire a refresh command to the pubnub function to get the leaderboard to update
        await PublishMessage("{\"username\":\"\",\"score\":\"\",\"refresh\":\"true\"}", PubNubUtilities.chanLeaderboardPub);
    }

    /// <summary>
    /// Publishes a Message to the PubNub Network
    /// </summary>
    /// <param name="text"></param>
    private async Task<bool> PublishMessage(string text, string channel)
    {
        PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
         .Channel(channel)
         .Message(text)
         .ExecuteAsync();

        if (publishResponse.Status.Error)
        {
            Debug.Log($"Error sending PubNub Message ({PubNubUtilities.GetCurrentMethodName()}): {publishResponse.Status.ErrorData.Information}");
        }

        return true;
    }

    /// <summary>
    /// Event listener to handle PubNub Message events
    /// </summary>
    /// <param name="result"></param>
    private void OnPnMessage(PNMessageResult<object> result)
    {
        // Enable the button once we have established connection to PubNub 
        if (result.Channel.Equals(PubNubUtilities.chanLeaderboardSub))
        {
            Dictionary<string, object> msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());// as Dictionary<string, object>;
                                                                                                                                  //Dictionary<string, object> msg = result.Message
            var usernames = (msg["username"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();
            var scores = (msg["score"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();

            if (usernames[0] != null)
            {
                namePos1.text = usernames[0];
                kdPos1.text = scores[0];
            }

            if (usernames[1] != null)
            {
                namePos2.text = usernames[1];
                kdPos2.text = scores[1];
            }

            if (usernames[2] != null)
            {
                namePos3.text = usernames[2];
                kdPos3.text = scores[2];
            }

            if (usernames[3] != null)
            {
                namePos4.text = usernames[3];
                kdPos4.text = scores[3];
            }

            if (usernames[4] != null)
            {
                namePos5.text = usernames[4];
                kdPos5.text = scores[4];
            }
        }
    }
}