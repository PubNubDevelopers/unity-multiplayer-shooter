using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

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
        public ChatSystem chatSystem;
        public Text chosenMapText;
        public Text chosenPlayerNumberText;
        public Text enableBotsText;
        public Text currentNumberOfPlayersInRoomText;
        public Button startBTN;

        // Internals:
        string randomRoomName;

        void OnEnable(){
            Connector.instance.onRoomListChange += onRoomListUpdate;
            Connector.instance.onCreateRoomFailed += onCreateRoomFailed;
            Connector.instance.onJoinRoom += OnJoinedRoom;
            Connector.instance.onLeaveRoom += OnLeftRoom;
            Connector.instance.onPlayerJoin += OnPlayerJoined;
            Connector.instance.onPlayerLeave += OnPlayerLeft;
        }
        void OnDisable(){
            Connector.instance.onRoomListChange -= onRoomListUpdate;
            Connector.instance.onCreateRoomFailed -= onCreateRoomFailed;
            Connector.instance.onJoinRoom -= OnJoinedRoom;
            Connector.instance.onLeaveRoom -= OnLeftRoom;
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
            if (Connector.instance.rooms.Count > 0)
            {
                listStatusText.text = "";
                for (int i = 0; i < Connector.instance.rooms.Count; i++)
                {
                    if (!PhotonNetwork.InRoom || (bool)Connector.instance.rooms[i].CustomProperties["isInMatchmaking"] == false)
                    {
                        RoomItem r = Instantiate(roomItemPrefab, roomItemHandler);
                        r.Set(Connector.instance.rooms[i], this);
                    }
                }
            }
            // else, just show an error text:
            else
            {
                listStatusText.text = "No games are currently available";
            }
        }
        public void RefreshPlayerList()
        {

            // Clear list first:
            foreach (Transform t in playerItemHandler)
            {
                Destroy(t.gameObject);
            }

            // Repopulate:
            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
            {
                CustomGamePlayerItem cgp = Instantiate(playerItemPrefab, playerItemHandler, false);
                cgp.Set(players[i]);
            }

            // Player number in room text:
            currentNumberOfPlayersInRoomText.text = "Players (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + ")";

            // Enable/disable start button:
            bool allowBots = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("customAllowBots") && (bool)PhotonNetwork.CurrentRoom.CustomProperties["customAllowBots"];
            startBTN.interactable = PhotonNetwork.IsMasterClient && ((players.Length > 1 && !allowBots) || (allowBots));
        }
        public void Join(RoomInfo room){
            Connector.instance.JoinCustomGame(room);
        }
        public void Leave()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
        public void Create(){
            Connector.instance.CreateCustomGame(mapSelector.curSelected, playerNumberSelector.items[playerNumberSelector.curSelected].value, enableBotsOption.isOn);
        }
        public void StartGame(){
            Connector.instance.StartCustomGame();
        }

        // Subscribed to Connector's "onRoomListChange" event:
        void onRoomListUpdate(int roomCount)
        {
            RefreshBrowser();
        }
        // Subscribed to Connector's "OnPlayerJoin" event:
        void OnPlayerJoined(Player player)
        {
            // When a player connects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            chatSystem.SendSystemChatMessage(player.NickName + " joined the game.", false);
        }
        // Subscribed to Connector's "onPlayerLeave" event:
        void OnPlayerLeft(Player player)
        {
            // When a player disconnects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            chatSystem.SendSystemChatMessage(player.NickName + " left the game.", true);
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

            chosenMapText.text = Connector.instance.maps[(int)PhotonNetwork.CurrentRoom.CustomProperties["map"]];
            chosenPlayerNumberText.text = PhotonNetwork.CurrentRoom.MaxPlayers.ToString();
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("customAllowBots")) enableBotsText.text = (bool)PhotonNetwork.CurrentRoom.CustomProperties["customAllowBots"]? "Yes" : "No";
        }
        // Subscribed to Connector's "onLeaveRoom" event:
        void OnLeftRoom(){
            if (PhotonNetwork.InRoom){
                PhotonNetwork.LeaveRoom();
            }
        }
    }
}