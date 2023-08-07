using PubNubUnityShowcase;
//using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Player List Item Item
    /// - The script for the populated item in the player search list
    /// </summary>

    public class PlayerListItem : MonoBehaviour
    {
        [Header("References:")]
        public Image profileImage;
        public Text nameText;
        public Button selectBtn;

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
            //For now, leaving the default profile Image. In the future, can grab the cached profile image when the ability to add profile images to is done.
            selectBtn.onClick.AddListener(() => OnPlayerClick());
        }

        /// <summary>
        /// Called when the user clicks on a player
        /// </summary>
        public void OnPlayerClick()
        {       
            //Use the dropdown from Chat.cs to trigger event handlers.
            Connector.instance.PlayerSelected("selected", userId);
        }
    }
}