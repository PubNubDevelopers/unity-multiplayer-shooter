using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Emote Popup
    /// - The script for the emote popup game object itself. Handles the showing/hiding, cooldown etc.
    /// </summary>

	[ExecuteInEditMode]
    public class EmotePopup : MonoBehaviour
    {
        public Sprite[] emotes;
        [Space]

        [Header("Settings:")]
        public float coolDown = 2;
        public float duration = 3;
        public Vector2 followOffset;
        public Vector2 screenBound;
        public Vector2 boundOffset;

        [Header("References:")]
        public Transform rootFlipper;
        public SpriteRenderer emoteRend;
        public Animator anim;
        public PlayerController owner;
        
        Vector2 finalBoundOffset;
        float curCD;
        bool isOurOwn;

        public bool isReady{
            get {
                return curCD <= 0;
            }
        }

        // Use this for initialization
        void Start()
        {
            // Hide on create:
            Hide();

            isOurOwn = owner.isPlayerOurs;
        }

		public void Show(int emoteID){
            // Show the graphics:
            rootFlipper.gameObject.SetActive(false);
            rootFlipper.gameObject.SetActive(true);
            anim.SetBool("close", false);

            // Display the right emote:
            emoteRend.sprite = emotes[emoteID];

            // Cool down:
            curCD = coolDown;

            // Hide after duration:
            Invoke("DoneShow", duration);
        }

        // Called by the animator:
        public void Hide(){
            rootFlipper.gameObject.SetActive(false);
        }

        void DoneShow(){
            anim.SetBool("close", true);
        }

        // Update is called once per frame
        void Update()
        {
            if (owner && rootFlipper.gameObject.activeSelf)
            {
                // Positioning:
                Vector3 finalPos = owner.transform.position + new Vector3(followOffset.x * rootFlipper.localScale.x, followOffset.y);
                transform.position = finalPos;

                // Flipping:
                if (!isOurOwn)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(owner.transform.position);
                    rootFlipper.localScale = new Vector3(screenPos.x < ((Screen.width - screenBound.x) / 2) ? 1 : -1, /*screenPos.y < ((Screen.height - screenBound.y) / 2) ? 1 : -1*/ 1, 1);
                    // Emote renderer flipping (since flipping the parent will cause the sprite renderer (child) to also flip, thus the following fix):
                    emoteRend.flipX = rootFlipper.localScale.x > 0;
                    emoteRend.flipY = rootFlipper.localScale.y > 0;
                }

                // Final bound offset:
                if (rootFlipper) finalBoundOffset = transform.position + new Vector3(boundOffset.x * rootFlipper.localScale.x, boundOffset.y * rootFlipper.localScale.y);

            }

            // Handling cooldown:
            if (curCD > 0)
            {
                curCD -= Time.deltaTime;
            }
        }

        void OnDrawGizmos(){
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(finalBoundOffset, screenBound);
        }
    }
}