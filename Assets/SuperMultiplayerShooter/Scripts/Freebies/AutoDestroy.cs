using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Auto Destroy
    /// - A very simple auto destroy/disable script
    /// </summary>

    public class AutoDestroy : MonoBehaviour
    {
        [Tooltip("Do not actually destroy but disable instead.")]
        public bool disableOnly;
        public float delay;

        void OnEnable()
        {
            if (disableOnly)
            {
                CancelInvoke();
                Invoke("Disable", delay);
            }
            else
            {
                Destroy(gameObject, delay);
            }
        }

        void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}