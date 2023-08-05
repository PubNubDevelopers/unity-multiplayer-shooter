using PubNubUnityShowcase;
//using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Room Item
    /// - The script for the selectable and populated item in the game browser list that shows a game's info
    /// </summary>

    public class FriendsListItem : MonoBehaviour
    {
        [Header("References:")]
        public Image onlineStatus; //TODO?
        public Text nameText;
        public Button messageButton;
        public Button tradeButton;
        public Button removeButton;

        private string userId;

        /// <summary>
        /// Set-up the Prefab contents.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="name"></param>
        /// <param name="image"></param>
        public void Set(string uuid, string name)
        {           
            nameText.text = name;
            userId = uuid;
            //For now, leaving the default profile Image. In the future, can grab the cached profile image when the ability to add profile images to is done.
            messageButton.onClick.AddListener(() => OnMessageClick());
            //tradeBtn.onClick.AddListener(() => OnTradeClick());
            removeButton.onClick.AddListener(() => OnRemoveClick());
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
        public void OnTradeClick()
        {
            //TODO, once plauyer trading is integrated.
        }
        /// <summary>
        /// Remove friend
        /// </summary>
        public void OnRemoveClick()
        {
            Destroy(gameObject);
            //TODO: Remove player from Friend Group using userId
        }
    }
}