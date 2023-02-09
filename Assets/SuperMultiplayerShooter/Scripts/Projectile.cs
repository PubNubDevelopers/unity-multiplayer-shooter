using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// Projectile
    /// - Uses raycast to check hits (this prevents skipping walls etc). Projectiles are spawned
    ///   locally so no network instantiation is involved.
    /// </summary>

    public class Projectile : MonoBehaviour
    {
        [Tooltip("Useful when making projectiles with larger body such as missiles, fire balls etc. Set 0 to ignore.")]
        public float hitRadius;
        public float speed;                                 // base speed
        public bool alwaysFaceMoveDirection;
        public bool keepVelocityAboveZero;
        [Tooltip("0 = does not explode")]
        public float explosionRadius;
        [Tooltip("The delay so that graphics such as smoke trails will still have time to fade out.")]
        public float destroyDelay;
        [Space]
        public GameObject spriteObj;
        public string bodyHitVFX;
        public string explosionVFX;
        public string obstacleHitVFX;
        public string shieldHitVFX;
        [HideInInspector] public float curAcceleration;
        [HideInInspector] public float lifetime;			// automatically destroy after this delay (0 = unlimited)
        [HideInInspector] public ObjectPooler pooler;
        [HideInInspector] public PlayerInstance owner;
        [HideInInspector] public int weaponId;				// set by the weapon who fired this. Although we can just get the weapon through the `owner`, that weapon still might change anytime.
        
        // Internals:
        float curSpeed;                                     // current speed we're gonna use to move this projectile (affected by the acceleration and x direction)
        Vector2 lastPos;                                    // used for checking if we hit something between the last and current frame (shooting a ray from lastPos to current position):
        bool thisIsMine;
        CameraController cam;
        float curLifetime;
        float curDestroyDelay;
        bool destroyNow;
        int xDir;
        bool destroyWhenNoOwner;
        bool shot;                                          // has the projectile already got out of player's collider?

        // Use this for initialization
        void Start()
        {
            // Reset last known position:
            RefreshLastPos();

            if (!cam)
                cam = FindObjectOfType<CameraController>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!destroyNow)
            {
                // Acceleration:
                curSpeed += xDir * curAcceleration * Time.deltaTime;
                if (keepVelocityAboveZero && curSpeed * xDir < 0) curSpeed = 0;

                // Movement:
                transform.Translate(transform.right * curSpeed * Time.deltaTime, Space.World);

                // Face direction of movement (if allowed):
                if (alwaysFaceMoveDirection) transform.localScale = new Vector3(curSpeed * xDir >= 0 ? xDir : -xDir, 1, 1);

                // Manage lifetime:
                if (lifetime > 0)
                {
                    if (curLifetime < lifetime)
                    {
                        curLifetime += Time.deltaTime;
                    }
                    else
                    {
                        // VFX
                        Transform hT = pooler.Spawn(obstacleHitVFX, transform.position, transform.localScale.x < 0 ? Quaternion.Inverse(transform.rotation) : transform.rotation).transform;

                        // Flip hit vfx along with this projectile:
                        hT.localScale = new Vector3(transform.localScale.x, 1, 1);

                        // Reset lifetime for later use:
                        lifetime = 0;
                        curLifetime = 0;

                        // Destroy this projectile:
                        destroyNow = true;
                    }
                }

                // Check hit using raycast:
                CheckHit();
                RefreshLastPos();

                // Destroy when outside the game area:
                float xbound = GameManager.instance.maps[GameManager.instance.chosenMap].bounds.x;
                float ybound = GameManager.instance.maps[GameManager.instance.chosenMap].bounds.y;
                float xoffset = GameManager.instance.maps[GameManager.instance.chosenMap].boundOffset.x;
                float yoffset = GameManager.instance.maps[GameManager.instance.chosenMap].boundOffset.y;

                if (transform.position.x > (xbound) + xoffset ||
                    transform.position.x < (-xbound) + xoffset ||
                    transform.position.y > (ybound) + yoffset ||
                    transform.position.y < (-ybound) + yoffset)
                {
                    destroyNow = true;
                }
            }
            // Destroy delay:
            else
            {
                // Hide the sprite:
                spriteObj.SetActive(false);

                // Then disable the entire object after the delay:
                if (curDestroyDelay > 0)
                {
                    curDestroyDelay -= Time.deltaTime;
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        void CheckHit()
        {
            // We hit something? Then do react!
            RaycastHit2D hit = hitRadius > 0 ? Physics2D.CircleCast(lastPos, hitRadius, new Vector2(transform.position.x, transform.position.y) - lastPos, (new Vector2(transform.position.x, transform.position.y) - lastPos).magnitude) : Physics2D.Raycast(lastPos, new Vector2(transform.position.x, transform.position.y) - lastPos, (new Vector2(transform.position.x, transform.position.y) - lastPos).magnitude);

            if (!shot){
                if (hit){
                    if (!hit.collider.CompareTag("Player"))
                    {
                        shot = true;
                    }
                    else{
                        PlayerController p = hit.collider.GetComponentInParent<PlayerController>();
                        if (!p || p.playerInstance.playerID != owner.playerID){
                            shot = true;
                        }
                    }
                }
            }
            
            if (shot)
            {
                if (hit && !destroyNow)
                {
                    // Let projectile pass through pickups and portals:
                    if (!hit.collider.CompareTag("Pickup") && !hit.collider.CompareTag("Portal"))
                    {
                        // If explodes then it doesn't matter what we hit, just explode:
                        if (explosionRadius > 0)
                        {
                            if (thisIsMine) Explode();

                            // VFX
                            pooler.Spawn(explosionVFX, hit.point);
                            
                            // Finally, destroy this projectile... "destroy" meaning deactivate, because we're still going to
                            // be using this later (ObjectPooler):
                            destroyNow = true;
                        }
                        // If doesn't explode:
                        else
                        {
                            // Do something when we hit a player:
                            if (hit.collider.CompareTag("Player"))
                            {
                                PlayerController p = hit.collider.GetComponentInParent<PlayerController>();
                                if (p)
                                {
                                    // Don't do anything on owner hit:
                                    if (p.playerInstance.playerID != owner.playerID)
                                    {
                                        // Only do vfx if invulnerable:
                                        if (p.invulnerable)
                                        {
                                            // VFX
                                            pooler.Spawn(shieldHitVFX, hit.point);
                                        }
                                        // Do vfx and damage if not:
                                        else
                                        {
                                            if (thisIsMine) p.ApplyDamage(owner.playerID, weaponId, true);
                                            // VFX
                                            pooler.Spawn(bodyHitVFX, hit.point);
                                        }

                                        // Finally, destroy this projectile... "destroy" meaning deactivate, because we're still going to
                                        // be using this later (ObjectPooler):
                                        destroyNow = true;
                                    }
                                }
                                else
                                {
                                    ObstacleHit(hit);
                                }
                            }
                            else
                            {
                                ObstacleHit(hit);
                            }
                        }
                    }
                }
            }
        }

        void ObstacleHit(RaycastHit2D hit){
            // VFX
            Transform hT = pooler.Spawn(obstacleHitVFX, hit.point, transform.localScale.x < 0 ? Quaternion.Inverse(transform.rotation) : transform.rotation).transform;

            // Hit effect orientation by surface hit normal:
            // NOTE: Uncomment this block of code if you want the hit effect to face away from the hit point:
            // ----------------------------------------------------------
            /*Transform hT = Instantiate (hitEffect, hit.point, Quaternion.identity).transform;
            Quaternion hitRot = Quaternion.FromToRotation (-hT.right, hit.normal);
            hT.rotation = hitRot;*/
            // ----------------------------------------------------------

            // Flip hit vfx along with this projectile:
            hT.localScale = new Vector3(transform.localScale.x, 1, 1);

            // Finally, destroy this projectile... "destroy" meaning deactivate, because we're still going to
            // be using this later (ObjectPooler):
            destroyNow = true;
        }

        void Explode()
        {
            // Damaging:
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].CompareTag("Player"))
                {
                    PlayerController p = cols[i].GetComponent<PlayerController>();
                    if (((p.playerInstance.playerID == owner.playerID && GameManager.instance.allowHurtingSelf) || p.playerInstance.playerID != owner.playerID) && !p.invulnerable)
                    {
                        if (!p.isDead)
                        {
                            Vector2 pos = (Vector2)transform.position;
                            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, new Vector2(cols[i].transform.position.x, cols[i].transform.position.y) - pos, explosionRadius);
                            RaycastHit2D hit = new RaycastHit2D();
                            for (int h = 0; h < hits.Length; h++)
                            {
                                if (hits[h].collider.gameObject == cols[i].gameObject)
                                {
                                    hit = hits[h];
                                    // Calculate the damage based on distance:
                                    int finalDamage = Mathf.RoundToInt(GameManager.instance.maps[GameManager.instance.chosenMap].spawnableWeapons[weaponId].damage * (1 - ((transform.position - new Vector3(hit.point.x, hit.point.y)).magnitude / explosionRadius)));
                                    // Apply damage:
                                    p.ApplyDamage(owner.playerID, finalDamage, false);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resets the projectile's properties for reuse.
        /// </summary>
        public void Reset(float projSpeed, float acceleration, int xDirection, float projectileLifetime, bool destroyWhenOwnerDies, PlayerInstance from, int idOfWeapon, ObjectPooler objectPooler)
        {
            speed = projSpeed;
            curSpeed = speed * xDirection;
            curAcceleration = acceleration;
            lifetime = projectileLifetime;
            xDir = xDirection;
            weaponId = idOfWeapon;
            owner = from;
            pooler = objectPooler;

            // other settings:
            thisIsMine = owner.isMine || (owner.isBot && PhotonNetwork.IsMasterClient);
            curDestroyDelay = destroyDelay;
            destroyNow = false;
            shot = false;
            spriteObj.SetActive(true);
            transform.localScale = new Vector3(xDirection, 1, 1);   // face the right direction...
            RefreshLastPos();
        }
        /// <summary>
        /// Updates the last known position of this projectile.
        /// </summary>
        public void RefreshLastPos()
        {
            lastPos = new Vector2(transform.position.x, transform.position.y);
        }

        void OnDrawGizmos(){

            // Hit radius:
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
            
            // Explosion radius:
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
