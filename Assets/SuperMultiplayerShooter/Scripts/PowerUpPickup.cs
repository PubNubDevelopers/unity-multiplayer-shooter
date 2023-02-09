using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Visyde
{
    /// <summary>
    /// Power Up Pickup
    /// - The component script for power-up pickup prefabs.
    /// </summary>

    public class PowerUpPickup : MonoBehaviourPunCallbacks
    {
        [Header("References:")]
        public SpriteRenderer itemGraphic;

        GameManager gm;
        PowerUp itemHandled;

        bool allowPickup = true;
        object[] data;

        // Use this for initialization
        void Start()
        {

            // References:
            gm = FindObjectOfType<GameManager>();

            // data from network (by master client):
            data = photonView.InstantiationData;

            itemHandled = gm.maps[gm.chosenMap].spawnablePowerUps[(int)data[0]];
            if ((int)data[1] != -1) itemHandled = gm.maps[gm.chosenMap].powerUpSpawnPoints[(int)data[1]].onlySpawnThisHere;

            // Visual:
            itemGraphic.sprite = itemHandled.icon;
        }

        void Update()
        {
            // Hide when someone already picked this pickup:
            itemGraphic.enabled = allowPickup;
        }

        void Allow()
        {
            allowPickup = true;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (allowPickup)
            {
                if (col.tag == "Player")
                {
                    PlayerController p = col.GetComponent<PlayerController>();
                    if (p)
                    {
                        allowPickup = false;

                        // Sound and VFX:
                        Instantiate(itemHandled.pickUpEffect, transform.position, Quaternion.identity);

                        // Only the master client will handle the power-up actions:
                        if (PhotonNetwork.IsMasterClient)
                        {
                            p.photonView.RPC("ReceivePowerUp", RpcTarget.AllViaServer, (int)data[0], (int)data[1]);
                            photonView.RPC("Picked", RpcTarget.AllViaServer);
                        }
                    }
                }
            }
        }

        [PunRPC]
        public void Picked()
        {
            gm.itemSpawner.PowerUpPickedUp((int)data[2]);

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(photonView);
            }
        }
    }
}
