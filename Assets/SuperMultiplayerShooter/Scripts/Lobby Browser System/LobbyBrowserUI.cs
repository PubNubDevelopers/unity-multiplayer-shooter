using PubNubUnityShowcase;
using System.Drawing;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Lobby Browser UI
    /// - The script for the sample lobby browser interface
    /// </summary>

    public class LobbyBrowserUI : MonoBehaviour
    {
        [Header("Browser:")]
        public float browserRefreshRate = 3f;   // how many times should the browser refresh itself
        public Transform roomItemHandler;		// this is where the room item prefabs will be spawned
        public RoomItem roomItemPrefab;         // the room item prefab (represents a game session in the lobby list)
        public Text listStatusText;             // displays the current status of the lobby browser (eg. "No games available", "Fetching game list...")

        [Header("Create Screen:")]
        public Text mapNameText;
        public SelectorUI mapSelector;
        public SelectorUI playerNumberSelector;
        public Toggle enableBotsOption;

        [Header("Joined Screen:")]
        public CustomGamePlayerItem playerItemPrefab;
        public Transform playerItemHandler;
        public Text messageDisplay;             // displays messages when joining/leaving lobby
        public Text chosenMapText;
        public Text chosenPlayerNumberText;
        public Text enableBotsText;
        public Text currentNumberOfPlayersInRoomText;
        public Button startBTN;
        public Button refreshLobbyBTN;
        public UnityEngine.Color lobbyColor;

        // Internals:
        string randomRoomName;

        void OnEnable(){
            Connector.instance.onRoomListChange += onRoomListUpdate;
            Connector.instance.onCreateRoomFailed += onCreateRoomFailed;
            Connector.instance.onJoinRoom += OnJoinedRoom;
            Connector.instance.onPlayerJoin += OnPlayerJoined;
            Connector.instance.onPlayerLeave += OnPlayerLeft;
        }
        void OnDisable(){
            Connector.instance.onRoomListChange -= onRoomListUpdate;
            Connector.instance.onCreateRoomFailed -= onCreateRoomFailed;
            Connector.instance.onJoinRoom -= OnJoinedRoom;
            Connector.instance.onPlayerJoin -= OnPlayerJoined;
            Connector.instance.onPlayerLeave -= OnPlayerLeft;
        }

        // Update is called once per frame
        void Update()
        {
            // ***CREATE***
            // Display selected map name:
            mapNameText.text = Connector.instance.maps[mapSelector.curSelected];
        }

        public void RefreshBrowser(){
            // Clear UI room list:
            foreach (Transform t in roomItemHandler)
            {
                Destroy(t.gameObject);
            }

            // If there are available rooms, populate the UI list:
            if (Connector.instance.pubNubRooms.Count > 0)
            {
                listStatusText.text = "";
                for (int i = 0; i < Connector.instance.pubNubRooms.Count; i++)
                {
                    if (!Connector.instance.InRoom)
                    {
                        RoomItem r = Instantiate(roomItemPrefab, roomItemHandler);
                        r.Set(Connector.instance.pubNubRooms[i], this);
                    }
                }
            }
            // else, just show an error text:
            else
            {
                listStatusText.text = "No games are currently available";
            }
        }
        public async void RefreshCurrentLobby()
        {
            //  Should work if not the master?
            await Connector.instance.PopulateRoomMembers();
        }
        public void RefreshPlayerList()
        {
            // Clear list first:
            foreach (Transform t in playerItemHandler)
            {
                Destroy(t.gameObject);
            }

            // Repopulate:
            if (Connector.instance.CurrentRoom != null)
            {
                PNPlayer[] players = Connector.instance.CurrentRoom.PlayerList.ToArray();
                for (int i = 0; i < players.Length; i++)
                {
                    CustomGamePlayerItem cgp = Instantiate(playerItemPrefab, playerItemHandler, false);
                    cgp.Set(players[i]);
                }
            }

            // Player number in room text:
            currentNumberOfPlayersInRoomText.text = "Players (" + Connector.instance.CurrentRoom.PlayerCount + "/" + Connector.instance.CurrentRoom.MaxPlayers + ")";

            // Enable/disable start button:
            bool allowBots = Connector.instance.CurrentRoom.AllowBots;
            startBTN.interactable = Connector.instance.isMasterClient && ((Connector.instance.CurrentRoom.PlayerList.Count > 1 && !allowBots) || (allowBots));
            refreshLobbyBTN.interactable = !Connector.instance.isMasterClient;
        }
        public void Join(PNRoomInfo room){
            Connector.instance.JoinCustomGame(room);
        }
        public void Leave()
        {
            if (Connector.instance.InRoom)
            {
                Connector.instance.LeaveRoom();
                //Send updates to Chat.cs
                Connector.instance.PlayerSelected("chat-remove", "Lobby");
            }
        }
        public async void Create(){
            await Connector.instance.CreateCustomGame(mapSelector.curSelected, playerNumberSelector.items[playerNumberSelector.curSelected].value, enableBotsOption.isOn);
        }
        public void StartGame(){
            Connector.instance.StartCustomGame();
        }
        public async void RefreshRooms()
        {
            //  Force a refresh of available Rooms from PubNub
            await Connector.instance.PubNubGetRooms();
            RefreshBrowser();
        }

        // Subscribed to Connector's "onRoomListChange" event:
        void onRoomListUpdate(int roomCount)
        {
            RefreshBrowser();
        }
        // Subscribed to Connector's "OnPlayerJoin" event:
        void OnPlayerJoined(PNPlayer player)
        {
            // When a player connects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            //Format color to be read in an HTML string.
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(lobbyColor);
            string message = $"<color={colorHex}>[{player.NickName}: joined the room]</color>\n";
            messageDisplay.text += message;
        }
        // Subscribed to Connector's "onPlayerLeave" event:
        void OnPlayerLeft(PNPlayer player)
        {
            // When a player disconnects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            //Format color to be read in an HTML string.
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(lobbyColor);
            string message = $"<color={colorHex}>[{player.NickName}: left the room]</color>\n";
            messageDisplay.text += message;
        }
        // Subscribed to Connector's "onCreateRoomFailed" event:
        void onCreateRoomFailed(){
            // Display error:
            DataCarrier.message = "Custom game creation failed.";
        }
        // Subscribed to Connector's "OnJoinRoom" event:
        void OnJoinedRoom()
        {
            // Update the player list when we join a room:
            RefreshPlayerList();

            chosenMapText.text = Connector.instance.maps[(int)Connector.instance.CurrentRoom.Map];
            chosenPlayerNumberText.text = Connector.instance.CurrentRoom.MaxPlayers.ToString();

            if (Connector.instance.CurrentRoom.AllowBots) enableBotsText.text = Connector.instance.CurrentRoom.AllowBots ? "Yes" : "No";

            //Send updates to Chat.cs
            Connector.instance.PlayerSelected("chat-add","Lobby");      
        }
    }
}