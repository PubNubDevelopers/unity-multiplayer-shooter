using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Visyde
{
    /// <summary>
    /// Jump Pad
    /// - The jump pad component. This requires a trigger collider to work.
    /// </summary>

    public class JumpPad : MonoBehaviourPun
    {
        public float force;                 // force amount
        public AudioClip launch;
        public AudioSource aus;

        void OnTriggerStay2D(Collider2D col)
        {
            if (col.CompareTag("Player"))
            {
                PlayerController p = col.GetComponent<PlayerController>();
                if (p)
                {
                    if (p.photonView.IsMine && !p.isOnJumpPad)
                    {
                        // Let the player know that they're on a jump pad:
                        p.isOnJumpPad = true;

                        // Apply the force:
                        Vector2 veloc = p.movementController.velocity;
                        veloc.y = force;
                        p.movementController.velocity = veloc;
                        photonView.RPC("Jumped", RpcTarget.All);
                    }
                }
            }
        }
        void OnTriggerExit2D(Collider2D col)
        {
            if (col.CompareTag("Player"))
            {
                PlayerController p = col.GetComponent<PlayerController>();
                if (p)
                {
                    if (p.photonView.IsMine) p.isOnJumpPad = false;
                }
            }
        }

        [PunRPC]
        public void Jumped()
        {
            // Sound:
            aus.PlayOneShot(launch);
        }
    }
}
