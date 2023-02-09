using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

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

		public Player owner;

        public void Set(Player player)
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
            kickBTN.gameObject.SetActive(PhotonNetwork.IsMasterClient && !owner.IsLocal);
        }

        public void Kick()
        {
            PhotonNetwork.CloseConnection(owner);
        }
    }
}