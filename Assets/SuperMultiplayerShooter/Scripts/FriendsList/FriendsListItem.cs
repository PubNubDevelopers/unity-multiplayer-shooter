using PubnubApi;
using PubNubUnityShowcase;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    public class FriendsListItem : MonoBehaviour
    {
        [Header("References:")]
        public Image onlineStatus;
        public Text nameText;
        public Button messageButton;
        public Button tradeButton;
        public Button acceptButton;
        public Button removeButton;
        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }

        private string userId;

        /// <summary>
        /// Set-up the Prefab contents.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="name"></param>
        public void Set(string uuid, string name)
        {
            nameText.text = name;
            userId = uuid;
            onlineStatus.color = Color.gray;
            messageButton.onClick.AddListener(() => OnMessageClick());
            acceptButton.onClick.AddListener(async () => await OnAcceptFriendClick());
            removeButton.onClick.AddListener(async () => await OnRemoveClick());
            tradeButton.onClick.AddListener(() => OnTradeClick());
        }

        /// <summary>
        /// Called when the user wants to send a private message to a friend.
        /// </summary>
        public void OnMessageClick()
        {
            Connector.instance.PlayerSelected("chat-add", userId);
        }
        /// <summary>
        /// Called when the user clicks the trade button
        /// </summary>
        public async void OnTradeClick()
        {
            //TODO: Will be integrated in a future update.
        }
        /// <summary>
        /// Remove friend
        /// </summary>
        public async Task<bool> OnRemoveClick()
        {
            Destroy(gameObject);

            //Determine if need to remove player from friend group.
            //remove = added to friend group, reject = yet to add friend to friend group.
            if (removeButton.name.Equals("remove"))
            {
                await PNManager.pubnubInstance.RemoveChannelsFromChannelGroup(PubNubUtilities.chanFriendChanGroupStatus + pubnub.GetCurrentUserId(), new string[] { PubNubUtilities.chanPresence + userId });
                await PNManager.pubnubInstance.RemoveChannelsFromChannelGroup(PubNubUtilities.chanFriendChanGroupChat + pubnub.GetCurrentUserId(), new string[] { PubNubUtilities.chanFriendChat + userId });
            }

            //Send reject request.
            string message = "reject";
            // Send Message to indicate request has been made.
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
               .Channel(PubNubUtilities.chanFriendRequest + userId) //chanFriendRequest channel reserved for handling friend requests.
               .Message(message)
               .ExecuteAsync();

            return true;
        }

        /// <summary>
        /// When you accept a pending friend invite.
        /// </summary>
        public async Task<bool> OnAcceptFriendClick()
        {
            //Hide accept and reject buttons. Allow for trade and remove friend.
            tradeButton.gameObject.SetActive(true);
            tradeButton.interactable = false; //To be removed once trading is fully integrated.
            acceptButton.gameObject.SetActive(false);
            removeButton.name = "remove"; // Used to determine whether or not to remove from friend groups
            gameObject.GetComponent<Image>().color = Color.white; // change color to show accepted friend.

            //Add to channel group.
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupStatus + pubnub.GetCurrentUserId(), new string[] { PubNubUtilities.chanPresence + userId });
            // Add friend to status feed group
            await PNManager.pubnubInstance.AddChannelsToChannelGroup(PubNubUtilities.chanFriendChanGroupChat + pubnub.GetCurrentUserId(), new string[] { PubNubUtilities.chanFriendChat + userId });

            string message = "accept"; // message will be one of four things: request, accept, reject, remove
            // Send Message to indicate request has been made.
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
               .Channel(PubNubUtilities.chanFriendRequest + userId) //chanFriendRequest channel reserved for handling friend requests.
               .Message(message)
               .ExecuteAsync();

            return true;
        }
    }
}