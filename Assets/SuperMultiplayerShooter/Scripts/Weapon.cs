using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
        }

        // Update is called once per frame
        void Update()
        {
            // Only do stuff when the game isn't over yet and in-game menu is hidden:
            if (!GameManager.instance.isGameOver && !GameManager.instance.ui.isMenuShown)
            {
                if (owner.photonView.IsMine)
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
                                        owner.aus.PlayOneShot(dry[Random.Range(0, dry.Length)]);

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

        // Called by the PlayerController (not by this weapon directly):
        public void Shoot()
        {
            curAmmo -= 1;

            // Sound:
            owner.aus.PlayOneShot(shoot[Random.Range(0, shoot.Length)]);

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
    }
}
