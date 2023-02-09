using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// AI Controller
    /// - controls the behavior of the bot and overrides the input for the PlayerController component.
    /// - This requires a PlayerController component to work. 
    /// </summary>

    public class AIController : MonoBehaviour
    {
        public int botID;										// used to identify who this bot is

        [Header("Settings:")]
        [Range(1, 10)]
        public int aggressiveness;                              // how aggressive the bot is. It can be treated as the "difficulty".
        [Range(1, 100)]
        public float sightRange;                                // how far can the AI see things
        public float maxJumpHeight;                             // the maximum height this bot can jump (defined by the MovementController of this player)
        public float maxJumpDistance;                           // the maximum jump distance horizontally
        public float sideGroundCheckerOffset;
        public float maxFloorAngle = 40f;                       // the maximum angle of a floor for it to be considered a floor
        public float wallCheckerDistance;
        public Vector2 colliderHeightChecker = Vector2.up;      // used to check the collider's height by shooting a ray down from the checker. This is the position of the checker relative to player's foot
        public string worldTag = "Untagged";
        public string pickupsTag = "Pickup";

        [Header("References:")]
        public BotSpawner botSpawnerPrefab;			            // Each bot owns one bot spawner. Bot spawners keep track of the death time and handles the respawning of the bot.

        // Controls:
        [HideInInspector] public Vector3 aimPos;
        [HideInInspector] public float xMovement;
        [HideInInspector] public bool doShoot;
        [HideInInspector] public bool doMeleeAttack;

        // Others:
        [HideInInspector] public Player owner;
        [HideInInspector] public PlayerController player;
        [HideInInspector] public PlayerController nearestPlayer;
        [HideInInspector] public WeaponPickup nearestWeapon;

        // Internals:
        Vector3 targetAimPos;                                                               // This is the latest aim position. The 'aimPos' variable's value is smoothly interpolated to this one to give a more natural aiming for the bot. 
        Vector3 wanderPos;                                                                  // the wander destination
        Vector2? lastLandHitPos;
        bool doneStep, readyToLand, doIdle;
        bool leftPlatform;                                                                  // sets to true when there's no more ground below after jumping off a platform
        float maxDistanceForMeleeAttack, colliderHeight, lastGroundedYPos, curSightRange;
        float? lastFoundPlatformHeight;
        float curReactDelay;                                                                // current delay before doing an attack (delay is based on aggressiveness value)
        float wanderDelay;
        int lastHp, doMoveDir;
        public int chosenEmote = -1;

        // Map bounds knowledge so we know where should we not go:
        float worldBoundXPos, worldBoundXNeg, worldBoundYPos, worldBoundYNeg;

        void OnValidate(){
            player = GetComponent<PlayerController>();
            if (!player)
            {
                Helper.LogError(this, "No PlayerController component attached.");

                Invoke("RemoveThis", 0.1f);
            }
        }

        void RemoveThis(){
            DestroyImmediate(GetComponent<AIController>());
        }

        // Use this for initialization
        void Start()
        {
            doneStep = true;

            // Ranges:
            maxDistanceForMeleeAttack = player.meleeWeapon.attackRange.x;

            // Know the bounds of the map:
            Vector3 bounds = GameManager.instance.maps[GameManager.instance.chosenMap].bounds;
            Vector3 offset = GameManager.instance.maps[GameManager.instance.chosenMap].boundOffset;
            worldBoundXPos = bounds.x + offset.x;
            worldBoundXNeg = -bounds.x + offset.x;
            worldBoundYPos = bounds.y + offset.y;
            worldBoundYNeg = -bounds.y + offset.y;

            // Sample emote AI usage:
            StartCoroutine("DoEmote");
        }

        // Bot initialization. This is called by the PlayerController component on spawn/start:
        public void InitializeBot(int id)
        {
            botID = id;  // this is now basically the same as the player ID of this bot

            // Create a bot spawner for this bot:
            if (!GameManager.instance.gameStarted)
            {
                BotSpawner bs = Instantiate(botSpawnerPrefab, new Vector3(), Quaternion.identity);
                bs.Initialize(botID, player);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (player.photonView.IsMine)
            {
                if (player)
                {
                    if (GameManager.instance.gameStarted && !GameManager.instance.isGameOver)
                    {
                        // Override the controls for our player controller:
                        player.nMousePos = aimPos;
                        player.xInput = xMovement;
                        player.shooting = curReactDelay >= 1 ? doShoot : false;
                        if (doMeleeAttack && curReactDelay >= 1) player.OwnerMeleeAttack();

                        // Adjust sight range depending on the currently equipped weapon's sight range:
                        curSightRange = player.curWeapon ? sightRange + player.curWeapon.sightRange : sightRange;

                        // AI logics:
                        aimPos = Vector3.MoveTowards(aimPos, targetAimPos, Time.deltaTime * aggressiveness * 5f);

                        // Step:
                        if (doneStep)
                        {
                            Invoke("DoStep", Random.Range(2f / aggressiveness, 4 / aggressiveness));
                            doneStep = false;
                        }

                        // Alert when getting hurt:
                        if (lastHp > player.health)
                        {
                            lastHp = player.health;
                            GotHurt();
                        }
                        else
                        {
                            lastHp = player.health;
                        }

                        // If there is a player nearby, react:
                        if (nearestPlayer)
                        {
                            // Attack:
                            Attack();
                        }
                        else
                        {
                            if (wanderDelay <= 0)
                            {
                                // Wander/Get a weapon:
                                DoMove(nearestWeapon && !HasWeaponAndAmmo() ? nearestWeapon.transform.position : wanderPos);
                                doShoot = false;
                                doMeleeAttack = false;
                            }
                            else
                            {
                                DoMove(wanderPos);
                            }
                        }
                        wanderDelay -= Time.deltaTime;
                    }
                    else
                    {
                        // No input when game over/before game starts:
                        player.xInput = 0;
                        player.shooting = false;
                    }
                }
            }

            // Manage AI reaction time:
            if (doShoot || doMeleeAttack)
            {
                if (curReactDelay < 1)
                {
                    curReactDelay += Time.deltaTime * aggressiveness * Random.Range(0.1f, 1);
                }
            }
            else
            {
                curReactDelay = 0;
            }
        }

        // Internal Actions:
        void DoStep()
        {
            // Get the nearest player:
            GetNearestPlayer();
            // Find a weapon (only when needed):
            if (!HasWeaponAndAmmo())
            {
                FindAWeapon();
            }

            // Generate a random wander destination:
            if (Random.Range(0, 5) > 2)
            {
                wanderPos = new Vector3(Random.Range(worldBoundXNeg, worldBoundXPos), Random.Range(worldBoundYNeg, worldBoundYPos));
            }
            if (!nearestPlayer)
            {
                if (Random.Range(0, 5) > 2)
                {
                    targetAimPos = new Vector3(Random.Range(worldBoundXNeg, worldBoundXPos), Random.Range(worldBoundYNeg, worldBoundYPos));
                }
            }

            doIdle = Random.Range(0, 11) > aggressiveness;

            doneStep = true;
        }
        void GetPlayerColliderHeight()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + new Vector3(colliderHeightChecker.x, colliderHeightChecker.y), Vector2.down);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.CompareTag("Player"))
                {
                    if (hits[i].collider.GetComponent<PlayerController>() == player) colliderHeight = hits[i].point.y - transform.position.y;
                }
            }
        }
        void GetNearestPlayer()
        {
            // Forget the current target (if we have one):
            nearestPlayer = null;

            // ...and try to find another one near us:
            float nearestDist = 0; // will be used to store the previous player's distance to our player to be compared to current
            GameManager gm = GameManager.instance;

            // Since we already have a list of currently existing player controllers, we can just iterate through it and find the nearest:
            for (int i = 0; i < gm.playerControllers.Count; i++)
            {
                PlayerController p = gm.playerControllers[i];

                // Make sure that the player we found isn't us:
                if (p != player)
                {
                    // Ignore if invulnerable:
                    if (!p.invulnerable)
                    {
                        if (!nearestPlayer)
                        {
                            // Set the first player in the list as the nearest (will be replaced by nearer players later down the loop):
                            nearestPlayer = p;
                            nearestDist = Helper.GetDistance(transform.position, p.transform.position);
                        }
                        else
                        {
                            // Get the distance of the current player from us and compare it to the last one:
                            float curDist = Helper.GetDistance(transform.position, p.transform.position);
                            if (curDist < nearestDist)
                            {
                                // Set it as the nearest if nearer:
                                nearestPlayer = p;
                                nearestDist = Helper.GetDistance(transform.position, p.transform.position);
                            }
                        }
                    }
                }
            }
        }
        void FindAWeapon()
        {
            // Forget the last one we found:
            nearestWeapon = null;

            // Get all weapon pickups within sight range:
            Collider2D[] cols = Physics2D.OverlapCircleAll(player.weaponHandler.position, curSightRange);

            // Iterate through the list of findings (if there are) and get the nearest one:
            if (cols.Length > 0)
            {
                float nearestW = 0;

                for (int i = 0; i < cols.Length; i++)
                {
                    if (cols[i].CompareTag(pickupsTag))
                    {
                        WeaponPickup wp = cols[i].GetComponent<WeaponPickup>();
                        if (wp)
                        {
                            if (!nearestWeapon)
                            {
                                nearestW = Helper.GetDistance(transform.position, wp.transform.position);
                                nearestWeapon = wp;
                            }
                            else
                            {
                                if (Helper.GetDistance(transform.position, wp.transform.position) < nearestW)
                                {
                                    nearestW = Helper.GetDistance(transform.position, wp.transform.position);
                                    nearestWeapon = wp;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Actions:
        void Attack()
        {
            if (player.isDead) return;  // Don't proceed if dead

            // Check if we're close enough that we can just melee-attack our target:
            if (Helper.GetDistance(transform.position, nearestPlayer.transform.position) <= player.meleeWeapon.attackRange.x)
            {
                // ...then attack:
                doMeleeAttack = true;
                doShoot = false;
                xMovement = 0;
            }
            // ...else, check if we have a weapon and some ammo to shoot the target:
            else
            {
                if (HasWeaponAndAmmo())
                {
                    // If we can see the target, shoot them:
                    if (CanSeeTargetPlayer())
                    {
                        // aim:
                        targetAimPos = nearestPlayer.weaponHandler.position;
                        // and shoot:
                        doShoot = true;
                        doMeleeAttack = false;
                        xMovement = 0;

                        // Sample emote on attack:
                        if (chosenEmote < 0) chosenEmote = Random.Range(0, 3);
                    }
                    // If not, then just wander/find a weapon:
                    else
                    {
                        doShoot = false;
                        doMeleeAttack = false;
                        DoMove(nearestWeapon && wanderDelay <= 0 ? nearestWeapon.transform.position : wanderPos);
                    }
                }
                // but if we don't have any, just try and chase our target, OR find a weapon:
                else
                {
                    if (chosenEmote < 0) chosenEmote = 1;

                    doMeleeAttack = false;
                    doShoot = false;

                    // If we have both a target and a weapon pickup nearby, try to chase the closer one:
                    if (nearestWeapon)
                    {
                        // If target player is nearer, chase them:
                        if (Helper.GetDistance(transform.position, nearestPlayer.transform.position) < Helper.GetDistance(transform.position, nearestWeapon.transform.position))
                        {
                            DoMove(wanderDelay <= 0 ? nearestPlayer.transform.position : wanderPos);
                        }
                        // If weapon pickup is nearer, get pickup:
                        else
                        {
                            DoMove(wanderDelay <= 0 ? nearestWeapon.transform.position : wanderPos);

                            // Sample emote on rush when getting a weapon:
                            if (chosenEmote < 0) chosenEmote = Random.Range(0, 3);
                        }
                    }
                    else
                    {
                        DoMove(wanderDelay <= 0 ? nearestPlayer.transform.position : wanderPos);
                    }
                }
            }
        }
        void Jump()
        {
            if (player.movementController.isGrounded)
            {
                lastGroundedYPos = transform.position.y;
                player.Jump();
            }
        }
        void DoMove(Vector3 destination)
        {
            if (!player.movementController.hasRigidbody) return;
            
            // Move on ground:
            if (player.movementController.isGrounded)
            {
                readyToLand = false;
                leftPlatform = false;
                lastFoundPlatformHeight = null;
                lastLandHitPos = null;

                // If decided to just stand still, then don't move:
                if (doIdle && player.curWeapon)
                {
                    doMoveDir = 0;
                }
                else
                {
                    // If we have a ground ahead:
                    if (SideHasGround(doMoveDir))
                    {
                        // Wander if arrived at destination x:
                        if (Mathf.Abs(destination.x - transform.position.x) < 1 && (destination.y > (transform.position.y + colliderHeight) || destination.y < (transform.position.y - colliderHeight)))
                        {
                            wanderDelay = Random.Range(1f, 2f);
                        }
                        else
                        {
                            // If there's a wall:
                            if (SideHasWall(doMoveDir))
                            {
                                // ...jump if we can jump onto it:
                                if (CanJumpUpOntoAPlatform(doMoveDir))
                                {
                                    Jump();
                                }
                                // else, turn around:
                                else
                                {
                                    doMoveDir *= -1;     // reverse movement direction
                                }
                            }
                            // If there's no wall:
                            else
                            {
                                // If destination is above us, try to jump up on a platform:
                                if (destination.y > transform.position.y && CanJumpUpOntoAPlatform(doMoveDir))
                                {
                                    Jump();
                                }
                                // ... else, continue running forward
                                else
                                {
                                    doMoveDir = destination.x > transform.position.x ? 1 : -1;     // move to destination's direction
                                }
                            }
                        }
                    }
                    // If we're on a dead end:
                    else
                    {
                        // If it's a gap, jump over it:
                        if (SideHasGap(doMoveDir))
                        {
                            Jump();
                        }
                        // If it's not a gap, there might be a platform we can jump onto:
                        else
                        {
                            // Check if there is and jump on it:
                            if (CanJumpUpOntoAPlatform(doMoveDir))
                            {
                                doMoveDir = destination.x > transform.position.x ? 1 : -1;     // move to destination's direction
                                Jump();
                            }
                            // If there's nothing:
                            else
                            {
                                doMoveDir *= -1;
                            }
                        }
                    }
                }
            }
            // Try to land:
            else
            {
                // Check if we have already left the previous platform:
                if (!leftPlatform)
                {
                    leftPlatform = player.movementController.velocity.y <= 0 && !SideHasGround(Mathf.RoundToInt(doMoveDir) * -1) && transform.position.y >= (lastFoundPlatformHeight.HasValue ? lastFoundPlatformHeight.Value : transform.position.y);
                }
                // Find a landing ground if we have already left the previous platform:
                else
                {
                    if (!readyToLand)
                    {
                        if (doMoveDir == 0) doMoveDir = 1;

                        // If we can land forward:
                        if (CanLandSide(doMoveDir))
                        {
                            readyToLand = true;
                            doMoveDir = destination.x > transform.position.x ? 1 : -1;     // movement direction to destination
                        }
                        // ...but if we cannot land forward, just go back by reversing the direction of movement:
                        else
                        {
                            doMoveDir = destination.x > transform.position.x ? -1 : 1;     // reverse direction
                        }
                    }
                    else
                    {
                        // If we are already in the destination mid-air, stop moving:
                        if (Mathf.Abs(transform.position.x - destination.x) < 0.1f)
                        {
                            doMoveDir = 0;
                        }
                        else
                        {
                            // Stop moving if there's already a ground below that we can land onto:
                            if (HasGroundBelow())
                            {
                                doMoveDir = 0;
                            }
                            // Else, keep moving:
                            else
                            {
                                if (lastLandHitPos.HasValue) doMoveDir = lastLandHitPos.Value.x > transform.position.x ? 1 : -1;    // Go to the landing area
                            }
                        }
                    }
                }
            }

            // Apply the movement direction to the main movement variable:
            xMovement = doMoveDir;

            // Look at destination:
            targetAimPos = destination;
        }

        // World checkers:
        RaycastHit2D CheckCeiling(float height)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.up, height);

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.CompareTag(worldTag))
                    {
                        return hits[i];
                    }
                }
            }
            return new RaycastHit2D();
        }
        bool SideHasGround(int whichSide)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + new Vector3(sideGroundCheckerOffset * whichSide, 0.01f, 0), Vector2.down, maxJumpHeight);
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.CompareTag(worldTag))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        bool HasGroundBelow()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, Mathf.Infinity);
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.CompareTag(worldTag) && Vector2.Angle(hits[i].normal, Vector2.up) <= maxFloorAngle)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        bool CanLandSide(int whichSide)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, (transform.position + new Vector3(whichSide, -1, 0)) - transform.position, Mathf.Infinity);

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.CompareTag(worldTag))
                    {
                        lastLandHitPos = hits[i].point;
                        float groundAngle = Vector2.Angle(hits[i].normal, Vector2.up);
                        return groundAngle <= maxFloorAngle;
                    }
                }
            }
            return false;
        }
        bool SideHasGap(int whichSide)
        {
            for (float i = 0; i < maxJumpDistance; i += 0.05f)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + new Vector3(sideGroundCheckerOffset + (i * whichSide), 0, 0), Vector2.down, maxJumpHeight);
                if (hits.Length > 0)
                {
                    for (int i2 = 0; i2 < hits.Length; i2++)
                    {
                        if (hits[i2].collider.CompareTag(worldTag))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        bool SideHasWall(int whichSide)
        {
            for (float i = 0; i < colliderHeight; i += 0.05f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position + (Vector3.up * i), transform.right * whichSide, wallCheckerDistance);
                if (hit && !hit.collider.isTrigger) return true;
            }
            return false;
        }
        bool CanJumpUpOntoAPlatform(int whichSide)
        {
            Vector2? foundAWall = null;
            float ceilingHeight = 0f;
            float lastFreeRay = 0f;
            float platformHeight = 0f;

            // First, check if there's a platform above us that can interfere with the jumping:
            RaycastHit2D ceilingHit = CheckCeiling(Mathf.RoundToInt(maxJumpHeight));
            if (ceilingHit.collider)
            {
                ceilingHeight = ceilingHit.point.y;
            }
            else
            {
                // There's no ceiling:
                ceilingHeight = maxJumpHeight;
            }

            // Now check for platforms horizontally:
            for (float i = 0f; i <= maxJumpHeight; i += 0.05f)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * i, new Vector2(whichSide, 0), wallCheckerDistance);
                if (hits.Length > 0)
                {
                    for (int i2 = 0; i2 < hits.Length; i2++)
                    {
                        if (hits[i2].collider.CompareTag(worldTag))
                        {
                            // Check if there's a platform/wall that intersects the current ray.
                            // If we have one, we can now start finding the top of the platform:
                            if (!foundAWall.HasValue)
                            {
                                foundAWall = hits[i2].point;
                            }
                        }
                    }
                }
                else
                {
                    // Finding the top of the platform:
                    if (foundAWall.HasValue)
                    {
                        // If currently there's no hit and we already found a hit before, we may now be on the top of a platform/wall, so save this Y position
                        // so we can use this to get the height of the platform:
                        lastFreeRay = i;
                    }
                }
            }

            // Then check vertically (platform height):
            if (foundAWall.HasValue && lastFreeRay != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(foundAWall.Value.x + (whichSide * 0.01f), transform.position.y + lastFreeRay), Vector2.down);
                if (hit)
                {
                    if (hit.point.y <= (transform.position.y + maxJumpHeight))
                    {
                        platformHeight = hit.point.y;
                        lastFoundPlatformHeight = platformHeight;

                        // Now we're done. Return true if our player collider can fit between the ceiling and the platform that we're jumping onto and
                        // that the ceiling isn't lower than the platform, return false otherwise:
                        return ceilingHeight - platformHeight >= colliderHeight && ceilingHeight > platformHeight;
                    }
                }
            }

            return false;
        }
        bool CanSeeTargetPlayer()
        {
            if (nearestPlayer)
            {
                Vector2 sightOrigin = player.weaponHandler.position;
                Vector2 sightTarget = nearestPlayer.weaponHandler.position;

                RaycastHit2D[] hits = Physics2D.RaycastAll(sightOrigin, sightTarget - sightOrigin, curSightRange);
                if (hits.Length > 0)
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].collider.gameObject != gameObject)
                        {
                            if (hits[i].collider.gameObject == nearestPlayer.gameObject)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        // Self checkers:
        bool HasWeaponAndAmmo()
        {
            if (player.curWeapon)
            {
                if (player.curWeapon.curAmmo > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // Others:
        void GotHurt()
        {
            doIdle = false;
        }
        
        // Sample emote AI usage:
        IEnumerator DoEmote()
        {
            while (true)
            {
                if (chosenEmote > 0)
                {
                    player.photonView.RPC("Emote", RpcTarget.All, 0);
                    chosenEmote = -1;
                }
                yield return new WaitForSecondsRealtime(Random.Range(1f, 6f));
            }
        }
    }
}