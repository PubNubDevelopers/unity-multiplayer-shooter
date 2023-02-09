using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Floating Bar
    /// - component for floating health bar which handles the health bar and player name display as well as the fire rate indicator
    /// </summary>

    public class FloatingBar : MonoBehaviour
    {
        public PlayerController owner;

        [Space]
        [Header("Settings:")]
        public float yOffset;
        public Color nameTextColorOwner = Color.white;
        public float colorFadeSpeed;

        [Header("References:")]
        public Text playerNameText;
        public Image hpFill;
        public Image shieldFill;
        public Slider rateOfFireIndicator;
        public GameObject hpBarObj;
        public GameObject shieldBarObj;
        public CanvasGroup cg;

        [HideInInspector] public GameManager gm;
        int lastHealth;

        void Start()
        {
            if (owner)
            {
                // Set text of name text to owner's name:
                playerNameText.text = owner.playerInstance.playerName;

                // Set name text color:
                playerNameText.color = owner.isPlayerOurs ? nameTextColorOwner : Color.white;

                // Show/Hide health bar:
                if (!owner.isPlayerOurs && !gm.showEnemyHealth)
                {
                    Destroy(hpBarObj);
                }
            }
        }

        void Update()
        {
            if (owner)
            {

                if (owner.isDead)
                {
                    Destroy(gameObject); // Destroy this when the owner dies.
                    return;
                }

                // HP fill amount:
                if (hpFill) hpFill.fillAmount = (float)owner.health / (float)owner.character.data.maxHealth;

                // Shield bar stuff (showing/hiding bar and handling the fill):
                if (shieldBarObj) shieldBarObj.SetActive(owner.shield > 0);
                if (shieldFill) shieldFill.fillAmount = (float)owner.shield / (float)owner.maxShield;

                // Fire rate indicator:
                if (owner.curWeapon)
                {
                    rateOfFireIndicator.gameObject.SetActive(owner.curWeapon.curFR < 1 && owner.curWeapon.curAmmo > 0 && owner.isPlayerOurs);
                    rateOfFireIndicator.value = owner.curWeapon.curFR;
                }

                if (owner.isDead)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Destroy(gameObject); // Destroy this if the owner doesn't exist anymore.
            }
        }

        void LateUpdate()
        {
            if (owner)
            {
                // Positioning:
                transform.position = Camera.main.WorldToScreenPoint(owner.transform.position + Vector3.up * yOffset);
            }
        }
    }
}