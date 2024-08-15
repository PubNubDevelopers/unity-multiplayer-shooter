using Newtonsoft.Json;
using PubnubApi;
using PubNubUnityShowcase;
using System;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Weapon
    /// - The component for weapon prefabs. Shooting is done in this script
    /// </summary>

    public class Weapon : MonoBehaviour
    {
        [Header("Settings:")]
        public int damage;
        public int ammo;
        public float rateOfFire;
        public int numberOfProjectilesPerShot = 1;
        public float projectileSpeed = 36;				// shot projectile speed
        [Range(-100, 100)]
        public float projectileAcceleration;
        public float spread;							// shot projectile spread
        public bool destroyProjectileWhenOwnerDies;
        public float sightRange = 3;                    // how far can the player see when handling this weapon
        public float kickbackAmount = 0.1f;				// how far/intense the rear movement of this weapon when fired
        [Tooltip("0 = don't destroy")]
        public float projectileLifetime;
        [Header("Sounds:")]
        public AudioClip[] shoot;
        public AudioClip[] dry;

        [Space]
        [Header("References:")]
        public Sprite hudIcon;                          // displayed in weapon hud
        public GameObject muzzleFlash;
        public Transform shootPoint;                    // where does the projectile spawn
        public SpriteRenderer spriteRenderer;
        [Tooltip("The projectile's name in the pooler.")]
        public string projectile;
        public PlayerController owner;

        // Internals:
        [HideInInspector] public int curAmmo;
        [HideInInspector] public float curFR;
        bool doneDrySound;

        void OnEnable()
        {
            muzzleFlash.SetActive(false);
        }

        void Start()
        {
            // allow immediate shoot on start:
            curFR = 1;

            // Set the current ammo to full on start:
            curAmmo = ammo;

            //Listeners
            PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;
        }

        // Update is called once per frame
        void Update()
        {
            // Only do stuff when the game isn't over yet and in-game menu is hidden:
            if (!GameManager.instance.isGameOver && !GameManager.instance.ui.isMenuShown)
            {
                if (owner.pubNubPlayerProps.IsMine)
                {
                    // Rate of fire:
                    if (curFR < 1)
                    {
                        curFR += Time.deltaTime * rateOfFire;
                    }

                    // Shooting:
                    if (owner.shooting)
                    {
                        if (curFR >= 1)
                        {
                            curFR = 0;

                            if (curAmmo > 0)
                            {
                                owner.OwnerShootCommand();
                            }
                            else
                            {
                                // make sure that current ammo count doesn't get pass below 0;
                                curAmmo = 0;

                                if (!doneDrySound)
                                {
                                    // Dry fire sound:
                                    if (dry.Length > 0)
                                        owner.aus.PlayOneShot(dry[UnityEngine.Random.Range(0, dry.Length)]);

                                    doneDrySound = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (curAmmo <= 0)
                        {
                            curAmmo = 0;
                        }
                        doneDrySound = false;
                    }
                }
            }

            // Weapon kickback (for visuals):
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime * 16f);
        }

        /// <summary>
        /// Called when the scene is changed. Remove any PubNub listeners.
        /// </summary>
        private void OnDestroy()
        {
            PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
        }

        // Called by the PlayerController (not by this weapon directly):
        public void Shoot()
        {
            curAmmo -= 1;

            // Sound:
            owner.aus.PlayOneShot(shoot[UnityEngine.Random.Range(0, shoot.Length)]);

            // Spawn projectile:
            for (int i = 0; i < numberOfProjectilesPerShot; i++)
            {
                Projectile p = GameManager.instance.pooler.Spawn(projectile, shootPoint.position, transform.rotation).GetComponent<Projectile>();

                // Let the projectile know what "x" direction to face:
                int xDir = owner.transform.localScale.x < 0? -1 : 1;

                // Setups:
                p.Reset(projectileSpeed, projectileAcceleration, xDir, projectileLifetime, destroyProjectileWhenOwnerDies, owner.playerInstance, owner.lastWeaponId, GameManager.instance.pooler);
                p.RefreshLastPos();
                // Projectile angle:
                p.transform.localEulerAngles = p.transform.localEulerAngles + new Vector3(0, 0, -spread + ((i + 0.5f) * ((spread * 2) / numberOfProjectilesPerShot)));
            }

            // Kick back:
            transform.localPosition = new Vector3(-kickbackAmount, 0, 0);

            // Muzzle flash VFX:
            muzzleFlash.SetActive(false);
            muzzleFlash.SetActive(true);
        }

        /// <summary>
        /// Event listener to handle PubNub Message events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnMessage(PNMessageResult<object> result)
        {
            //  There is one subscribe handler per weapon
            //  Adjusts the Rate of Fire and Damage for the CURRENTLY EQUIPPED weapon. Not a global update. Resets on new weapon pickup.
            if (result != null && result.Channel.StartsWith("illuminate"))
            {
                // Adjust Rate of Fire - resulting message is a percentage modifier
                if (result.Channel.Equals("illuminate.rof") && result.Message != null)
                {
                    Debug.Log($"Rate of Fire is being adjusted. Old rof: {rateOfFire}");
                    float modifier = float.Parse(result.Message.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    rateOfFire += (rateOfFire * modifier);
                    Debug.Log($"Rate of Fire is being adjusted. New rof: {rateOfFire}");
                }

                // Adjust Damage - resulting message is a percentage modifier
                else if (result.Channel.Equals("illuminate.damage") && result.Message != null)
                {
                    Debug.Log($"Damage is being adjusted. Old Dmg: {damage}");
                    float modifier = float.Parse(result.Message.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    damage += (int)Math.Round(damage * modifier);
                    Debug.Log($"Damage is being adjusted. New Dmg: {damage}");
                }

                // Adjust Kickback - resulting message is a percentage modifier
                else if (result.Channel.Equals("illuminate.kickback") && result.Message != null)
                {
                    Debug.Log($"Kickback is being adjusted. Old Kickback: {kickbackAmount}");
                    float modifier = float.Parse(result.Message.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    kickbackAmount += (kickbackAmount * modifier);
                    Debug.Log($"kickback is being adjusted. New kickback: {kickbackAmount}");
                }

                // Adjust Ammo - resulting message is a percentage modifier
                else if (result.Channel.Equals("illuminate.projectile_speed") && result.Message != null)
                {
                    Debug.Log($"Projectile Speed is being adjusted. Old Projectile Speed: {projectileSpeed}");
                    float modifier = float.Parse(result.Message.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    projectileSpeed += (projectileSpeed * modifier);
                    Debug.Log($"Projectile Speed is being adjusted. New Projectile Speed: {projectileSpeed}");
                }
            }
        }
    }
}
