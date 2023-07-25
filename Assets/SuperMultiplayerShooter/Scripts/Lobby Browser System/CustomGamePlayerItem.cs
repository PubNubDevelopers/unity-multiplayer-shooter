using UnityEngine;
using UnityEngine.UI;
using PubNubUnityShowcase;

namespace Visyde
{
    /// <summary>
    /// Custom Game Player Item
    /// - The script for the UI item that represents players in the custom game lobby
    /// </summary>

    public class CustomGamePlayerItem : MonoBehaviour
    {
        [Header("Settings:")]
        public Color ownerColor;

        [Header("References:")]
        public GameObject hostIndicator;
        public Text playerNameText;
        public Button kickBTN;

		public PNPlayer owner;

        public void Set(PNPlayer player)
        {
            owner = player;

            playerNameText.text = owner.NickName;
            if (owner.IsLocal)
            {
                playerNameText.color = ownerColor;
                kickBTN.gameObject.SetActive(false);
            }

            // Host indicator, position in list, and the kick buttons:
            hostIndicator.SetActive(owner.IsMasterClient);
            if (owner.IsMasterClient) transform.SetAsFirstSibling(); else transform.SetAsLastSibling();
            //  Note the ability to kick someone from a lobby has been removed for simplicity.
        }

        public void Kick()
        {
            //  Note the ability to kick someone from a lobby has been removed for simplicity.
        }
    }
}