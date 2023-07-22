using PubNubUnityShowcase;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Room Item
    /// - The script for the selectable and populated item in the game browser list that shows a game's info
    /// </summary>

    public class RoomItem : MonoBehaviour
    {
        [Header("Settings:")]
        public string openStatus;
        public string closedStatus;

        [Header("References:")]
        public Text statusText;
        public Text nameText;
        public Text mapText;
        public Text playerNumberText;
        public Button joinBTN;

        [HideInInspector] public PNRoomInfo info;
        LobbyBrowserUI lb;

        public void Set(PNRoomInfo theInfo, LobbyBrowserUI lobbyBrowser)
        {
            info = theInfo;
            lb = lobbyBrowser;

            // Labels:
            statusText.text = info.IsOpen ? openStatus : closedStatus;
            nameText.text = info.Name;
            mapText.text = Connector.instance.maps[info.Map];
            playerNumberText.text = info.PlayerCount + "/" + info.MaxPlayers;

			// Disable/enable join button:
            joinBTN.interactable = (info.PlayerCount < info.MaxPlayers && info.IsOpen);
        }

        public void Join(){
            lb.Join(info);
        }
    }
}