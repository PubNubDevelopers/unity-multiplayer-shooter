using UnityEngine;
using UnityEngine.UI;
//using Photon.Pun;
//using Photon.Realtime;

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

		public PubNubPlayer owner;

        public void Set(PubNubPlayer player)
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
            //  DCC todo Removed Kick logic for simplicity.
            //kickBTN.gameObject.SetActive(PhotonNetwork.IsMasterClient && !owner.IsLocal);
            //kickBTN.gameObject.SetActive(Connector.instance.isMasterClient && !owner.IsLocal);
        }

        //  DCC todo add comment: you could implement this by sending a message to the client instructing them to leave
        public void Kick()
        {
            //  DCC todo
            //PhotonNetwork.CloseConnection(owner);
        }
    }
}