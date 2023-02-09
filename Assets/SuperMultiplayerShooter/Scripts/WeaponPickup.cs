using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Visyde
{
    /// <summary>
    /// Weapon Pickup
    /// - The component script for weapon pickup prefabs.
    /// </summary>

    public class WeaponPickup : MonoBehaviourPun
    {
        [Header("References:")]
        public SpriteRenderer itemGraphic;
        public GameObject pickUpEffect;

        GameManager gm;
        Weapon itemHandled;

        bool allowPickup = true;
        object[] data;

        // Use this for initialization
        void Start()
        {

            // References:
            gm = FindObjectOfType<GameManager>();

            // data from network (by master client):
            data = photonView.InstantiationData;

            itemHandled = gm.maps[gm.chosenMap].spawnableWeapons[(int)data[0]];
            if ((int)data[1] != -1) itemHandled = gm.maps[gm.chosenMap].weaponSpawnPoints[(int)data[1]].onlySpawnThisHere;

            // Visual:
            itemGraphic.sprite = itemHandled.spriteRenderer.sprite;
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
                if (col.CompareTag("Player"))
                {
                    PlayerController p = col.GetComponent<PlayerController>();
                    if (p)
                    {
                        allowPickup = false;

                        // Sound and VFX:
                        Instantiate(pickUpEffect, transform.position, Quaternion.identity);

                        if (PhotonNetwork.IsMasterClient)
                        {
                            p.photonView.RPC("GrabWeapon", RpcTarget.AllViaServer, (int)data[0], (int)data[1]);
                            photonView.RPC("Picked", RpcTarget.All);
                            PhotonNetwork.Destroy(photonView);
                        }
                    }
                }
            }
        }

        [PunRPC]
        public void Picked()
        {
            gm.itemSpawner.WeaponPickedUp((int)data[2]);
        }
    }
}
