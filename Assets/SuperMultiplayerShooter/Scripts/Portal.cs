using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Visyde
{
    /// <summary>
    /// Portal
    /// - Can be configured to behave as a multi-way or enter/exit only portal.
    /// - If there are multiple exit points specified, a random one will be chosen every time.
    /// </summary>

    public class Portal : MonoBehaviourPun
    {
        [Tooltip("The portal to exit to. If there are more than one exit specified, this portal will choose a random one.")]
        public Portal[] exit;

        [Header("Settings:")]
        public Vector3 exitPoint;
        public string portalEnterVFX;
        public string portalExitVFX;
        [Header("References:")]
        public GameObject normal;
        public GameObject exitOnly;

        // In-game:
        public List<int> arriving { get; protected set; }                // ID's of the arriving players (used so that arriving players won't trigger the "portal enter" action once they enter this portal's collider)

        // Events:
        public delegate void PortalEvent (PlayerController player);
        public PortalEvent onPlayerEnter;
        public PortalEvent onPlayerArrived;

        // Edit mode:
        int exitLength = -1;

        void Awake(){
            arriving = new List<int>();

            RefreshGraphics();
        }

        void RefreshGraphics(){
            // Show the proper portal graphics:
            normal.SetActive(exit.Length > 0);
            exitOnly.SetActive(exit.Length == 0);
        }

        void OnTriggerEnter2D(Collider2D col){
            if (exit.Length > 0 && col.CompareTag("Player")){

                PlayerController player = col.GetComponent<PlayerController>();

                if (!player.photonView.IsMine) return;

                // Prevent arriving players from triggering the "portal enter" action and only allow those that aren't in the "arriving" list:
                if (!arriving.Contains(player.playerInstance.playerID)) photonView.RPC("TriggerPortal", RpcTarget.All, player.playerInstance.playerID);
            }
        }
        void OnTriggerExit2D(Collider2D col){
            if (col.CompareTag("Player"))
            {
                PlayerController player = col.GetComponent<PlayerController>();

                // Exiting the portal collider should allow player to reuse the portal:
                if (arriving.Contains(player.playerInstance.playerID)) arriving.Remove(player.playerInstance.playerID);
            }
        }

        [PunRPC]
        void TriggerPortal(int playerID){
            DoEnterPortal(GameManager.instance.GetPlayerControllerOfPlayer(GameManager.instance.GetPlayerInstance(playerID)));
        }

        void DoEnterPortal(PlayerController player)
        {
            // Do vfx:
            DoPortalEnterVFX();

            // Pick a random exit portal if there are multiple, pick first entry if there's only one:
            exit[exit.Length > 1 ? Random.Range(0, exit.Length) : 0].DoPortalArrive(player);

            // Do events:
            if (onPlayerArrived != null) onPlayerEnter.Invoke(player);
        }
        public void DoPortalArrive(PlayerController player){

            // Let this destination portal know who will arrive in advance to prevent triggering the "portal enter" action:
            arriving.Add(player.playerInstance.playerID);

            // Teleport the player:
            player.Teleport(transform.position + exitPoint);

            // Do vfx:
            DoPortalExitVFX();

            // Do events:
            if (onPlayerArrived != null) onPlayerArrived.Invoke(player);
        }

        public void DoPortalEnterVFX(){
            GameManager.instance.pooler.Spawn(portalEnterVFX, exitPoint + transform.position);
        }
        public void DoPortalExitVFX(){
            GameManager.instance.pooler.Spawn(portalExitVFX, exitPoint + transform.position);
        }

        void OnDrawGizmos(){
            if (exit.Length > 0){
                Vector3 thisPos = transform.position;
                Gizmos.color = Color.cyan;
                for (int i = 0; i < exit.Length; i++){
                    if (exit[i]) Gizmos.DrawLine(thisPos, exit[i].transform.position + exit[i].exitPoint);
                }
            }

            // Display portal graphics in edit mode:
            if (GameManager.instance) return;
            if (exitLength != exit.Length){
                exitLength = exit.Length;

                RefreshGraphics();
            }
        }
    }
}