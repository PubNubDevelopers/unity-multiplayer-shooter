using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Footsteps Audio Manager
    /// - plays random footsteps sounds called by an animation event so this is usually put on a 
    /// game object with an Animator component.
    /// </summary>

    public class FootstepsAudioManager : MonoBehaviour
    {
        public PlayerController player;

        public void Step()
        {
            // Randomly choose a footstep sound from the character data and play it one shot:
            player.aus.PlayOneShot(player.character.data.footstepSFX[Random.Range(0, player.character.data.footstepSFX.Length)]);
        }
    }
}