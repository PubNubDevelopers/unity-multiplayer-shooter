using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// Player Controller
    /// - The player controller itself! Requires a 2D character controller (like the MovementController.cs) to work.
    /// </summary>

    public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable, IInRoomCallbacks
    {
        public bool forPreview = false;                                 // used for non in-game such as character customization preview in the main menu

        [System.Serializable]
        public class Character
        {
            public CharacterData data;
            public Animator animator;

            // For cosmetics:
            public Transform hatPoint;
        }
        public Character[] characters;                                  // list of characters for the spawnable characters (modifying this will not change the main menu character selection screen
                                                                        // NOTE: please read the manual to learn how to add and remove characters from the character selection screen)

        [Space]
        [Header("Settings:")]
        public int maxShield;
        public string grenadePrefab;
        public float grenadeThrowForce = 20;

        [Header("References:")]
        public AIController ai;									        // the AI controller for this player (only gets enabled if this is a bot, disabled when not)
        public AudioSource aus;                                         // the AudioSource that will play the player sounds
        public MovementController movementController;                   // the one that controls the rigidbody movement
        public Transform weaponHandler;                                 // the transform that holds weapon prefabs
        public Transform grenadePoint;                                  // where grenades spawn
        public MeleeWeaponController meleeWeapon;                       // the melee weapon controller
        public CosmeticsManager cosmeticsManager;                       // the component that manages the cosmetic side of things
        public GameObject invulnerabilityIndicator;				        // shown when player is invulnerable
        public AudioClip[] hurtSFX;                                     // audios that are played randomly when getting damaged
        public AudioClip throwGrenadeSFX;               		        // audio that's played when throwing grenades
        public GameObject spawnVFX;								        // the effect that's shown on spawn
        public EmotePopup emotePopupPrefab;                             // emote prefab to spawn

        // In-Game:
        public PlayerInstance playerInstance;
        public PlayerInstance lastDamageDealer { get; protected set; }  // the last player to damage us (used to track who killed us and etc.)
        public int curCharacterID { get; protected set; }               // determines which character is used for this player
        public int health { get; protected set; }                       // current health amount
        public int shield { get; protected set; }                       // current shield amount
        public int lastWeaponId { get; protected set; }                 // used when sending damage across the network so everyone knows what weapon were used (negative value means character id)
        public bool isDead { get; protected set; }                      // is this player dead?
        public Vector3 mousePos { get; protected set; }                 // the mouse position we're working on locally
        public Weapon curWeapon { get; protected set; }                 // the current "physical" weapon the player is holding
        public Weapon originalWeapon { get; protected set; }            // the current weapon's prefab reference
        public EmotePopup curEmote { get; protected set; }              // this player's own emote popup
        public int curGrenadeCount { get; protected set; }              // how much grenades left
        public int curMultikill { get; protected set; }                 // current multi kills
        [HideInInspector] public bool isOnJumpPad;				        // when true, jumping is disabled to not interfere with the jump pad
        [HideInInspector] public Vector3 nMousePos;                     // mouse position from network. We're gonna smoothly interpolate the mousePos' value to this one to prevent the jittering effect.
        [HideInInspector] public bool shooting;                         // are we shooting?
        [HideInInspector] public float xInput;				            // the X input for the movement controls (sent to other clients for animation speed control)
        float jumpProgress;                                             // longer press means higher jump
        float curInvulnerability;
        float curMeleeAttackRate;
        float curMultikillDelay = 1;
        bool moving;                                                    // are we moving on ground?
        bool isFalling;                                                 // are we falling? (can be used for something like a falling animation)
        bool lastFrameGrounded;                                         // used for spawning landing vfx
        bool doneDeadZone;										        // makes sure that DeadZoned() doesn't called repeatedly
        float lastGroundedTime;
        Vector3 lastAimPos;                             		        // used for mobile controls
        GameManager gm;                                                 // GameManger instance reference for simplicity

        // Network:
        Vector2 lastPos, networkPos;
        float lag;

        // Returns the chosen character:
        public Character character{
            get{
                return characters[curCharacterID];
            }
        }
        // Returns true if invulnerable:
        public bool invulnerable
        {
            get
            {
                return curInvulnerability < gm.invulnerabilityDuration;
            }
        }
        // Returns true if this is a bot:
        public bool isBot
        {
            get
            {
                return photonView.IsSceneView;
            }
        }
        // Check if this player is ours and not owned by a bot or another player:
        public bool isPlayerOurs
        {
            get {
                return !playerInstance.isBot && playerInstance.isMine;
            }
        }

        void Awake(){
            // Find essential references:
            gm = GameManager.instance;
        }
        public override void OnEnable(){
            if (gm)
            {
                // Add this to the player controllers list:
                gm.playerControllers.Add(this);
            }
        }
        public override void OnDisable(){
            if (gm)
            {
                // Unsubscibe to Controls Manager events (doesn't do anything if player isn't ours):
                gm.controlsManager.jump -= Jump;

                // Remove from the player controllers list
                gm.playerControllers.Remove(this);
            }
        }
        void Start()
        {
            if (forPreview) return;

            // Spawn VFX:
            Instantiate(spawnVFX, transform);

            // If this is a bot, we need to initialize it and get its bot index from its instantiation data:
            if (isBot)
            {
                ai.InitializeBot((int)photonView.InstantiationData[0]);
                ai.enabled = true;
            }
            else
            {
                ai.enabled = false;
            }

            // Reset player stats and stuff:
            RestartPlayer();

            // Create a floating bar and apply stats from chosen character data to player:
            gm.ui.SpawnFloatingBar(this);
            movementController.moveSpeed = character.data.moveSpeed;
            movementController.jumpForce = character.data.jumpForce;
            curGrenadeCount = character.data.grenades;

            // Apply the cosmetics:
            cosmeticsManager.Refresh(playerInstance.cosmeticItems);

            // Spawn our own emote popup:
            curEmote = Instantiate(emotePopupPrefab, Vector3.zero, Quaternion.identity);
            curEmote.owner = this;

            // If we're the local player, let the camera know:
            if (playerInstance.isMine)
            {
                gm.gameCam.target = this;
            }

            // Let the movement controller know how to behave:
            movementController.isMine = photonView.IsMine;

            // Equip the starting weapon (if our current character has one):
            EquipStartingWeapon();

            if (photonView.IsMine)
            {
                // Setting up send rates:
                PhotonNetwork.SendRate = 16;
                PhotonNetwork.SerializationRate = 16;
            }
        }
        void Update()
        {
            if (forPreview) return;

            Transform t = transform;

            if (!isDead)
            {
                // Manage invulnerability:
                // *When invulnerable:
                if (curInvulnerability < gm.invulnerabilityDuration)
                {
                    if (gm.gameStarted) curInvulnerability += Time.deltaTime;

                    // Show the invulnerability indicator:
                    invulnerabilityIndicator.SetActive(true);
                }
                // *When not:
                else
                {
                    // Hide invulnerability indicator when finally vulnerable:
                    if (invulnerabilityIndicator.activeSelf) invulnerabilityIndicator.SetActive(false);
                }

                // Check if we're currently falling:
                isFalling = movementController.velocity.y < 0;


                // If owned by us (including bots):
                if (photonView.IsMine)
                {
                    // Dead zone interaction:
                    if (gm.deadZone)
                    {
                        if (t.position.y < gm.deadZoneOffset && !doneDeadZone)
                        {
                            photonView.RPC("TriggerDeadZone", RpcTarget.All, movementController.position);
                            doneDeadZone = true;
                        }
                    }

                    // *For our player:
                    if (isPlayerOurs)
                    {
                        HandleInputs();
                    }
                    // *For the bots:
                    else
                    {
                        if (!gm.isGameOver)
                        {
                            // Smooth mouse aim sync for the bot:
                            mousePos = nMousePos;
                        }
                    }

                    // Melee attack rate:
                    if (curMeleeAttackRate < 1)
                    {
                        curMeleeAttackRate += Time.deltaTime * meleeWeapon.attackSpeed;
                    }

                    // Multikill timer:
                    if (curMultikillDelay > 0)
                    {
                        curMultikillDelay -= Time.deltaTime;
                    }
                    else
                    {
                        curMultikill = 0;
                    }
                }
                else
                {
                    // Smooth mouse aim sync:
                    mousePos = Vector3.MoveTowards(mousePos, nMousePos, Time.deltaTime * (mousePos - nMousePos).magnitude * 15);
                }

                // Apply movement input to the movement controller:
                movementController.InputMovement(xInput);

                // Landing VFX:
                if (movementController.isGrounded)
                {
                    if (!lastFrameGrounded && (Time.time - lastGroundedTime) > 0.1f)
                    {
                        Land();
                    }
                    lastFrameGrounded = movementController.isGrounded;
                    lastGroundedTime = Time.time;
                }
                else
                {
                    lastFrameGrounded = movementController.isGrounded;
                }

                // Hide gun if attacking with melee weapon:
                weaponHandler.gameObject.SetActive(!meleeWeapon.isAttacking);

                // Flipping:
                t.localScale = new Vector3(mousePos.x > t.position.x ? 1 : mousePos.x < t.position.x ? -1 : t.localScale.x, 1, 1);

                // Since we're syncing everyone's mouse position across the network, we can just do the aiming locally:
                Vector3 diff = mousePos - weaponHandler.position;
                diff.Normalize();
                float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                weaponHandler.rotation = Quaternion.Euler(0f, 0f, rot_z + (t.localScale.x == -1 ? 180 : 0));
            }

            // Handling death:
            if (health <= 0 && !isDead)
            {
                isDead = true;
                
                if (!gm.isGameOver)
                {
                    // Remove any weapons:
                    DisarmItem();
                }

                // Update the others about our status:
                if (photonView.IsMine)
                {
                    photonView.RPC("UpdateOthers", RpcTarget.All, health, shield);
                    
                    // If this is local player's, let the game manager know this is ours and is now dead:
                    if (isPlayerOurs && !gm.isGameOver) gm.dead = true;
                }
                Die();
            }

            // Animations:
            if (character.animator)
            {
                character.animator.SetBool("Moving", moving);
                character.animator.SetBool("Dead", isDead);
                character.animator.SetBool("Falling", isFalling);

                // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
                character.animator.speed = moving && movementController.isGrounded ? Mathf.Abs(xInput) : 1;
            }
        }
        void FixedUpdate(){
            if (forPreview) return;

            // Simple lag compensation (movement interpolation + movement controller velocity set at OnPhotonSerializeView()):
            if (!photonView.IsMine && movementController){
                movementController.position = Vector2.MoveTowards(movementController.position, networkPos, Time.fixedDeltaTime * (lag * 10));
            }
        }

        void HandleInputs(){
            // Is moving on ground?:
            moving = movementController.velocity.x != 0 && movementController.isGrounded && xInput != 0;

            // Only allow controls if the menu is not shown (the menu when you press 'ESC' on PC):
            if (!gm.ui.isMenuShown)
            {
                // Example emote keys (this is just a hard-coded example of displaying an emote using alphanumeric keys
                //  so you may have to implement a more robust emote input system depending on your project's needs):
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    photonView.RPC("Emote", RpcTarget.All, 0);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    photonView.RPC("Emote", RpcTarget.All, 1);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    photonView.RPC("Emote", RpcTarget.All, 2);
                }

                // Player controls:
                if (gm.gameStarted && !gm.isGameOver)
                {
                    // Mouse position on screen or Joystick value if mobile (will be sent across the network):
                    if (gm.useMobileControls)
                    {
                        // Mobile joystick:
                        lastAimPos = new Vector3(gm.controlsManager.aimX, gm.controlsManager.aimY, 0).normalized;
                        mousePos = lastAimPos + new Vector3(transform.position.x, weaponHandler.position.y, 0);
                    }
                    else
                    {
                        // PC mouse:
                        mousePos = gm.gameCam.theCamera.ScreenToWorldPoint(Input.mousePosition);
                    }

                    // Horizontal movement input:
                    xInput = gm.useMobileControls ? gm.controlsManager.horizontal : gm.controlsManager.horizontalRaw;

                    // Shooting:
                    shooting = gm.controlsManager.shoot;

                    // Melee:
                    if (!gm.useMobileControls && Input.GetButtonDown("Fire2"))
                    {
                        OwnerMeleeAttack();
                    }

                    // Grenade throw:
                    if (!gm.useMobileControls && Input.GetButtonDown("Fire3"))
                    {
                        OwnerThrowGrenade();
                    }
                }
                else
                {
                    // Reset movement inputs when game is over:
                    xInput = 0;
                }
            }
            else
            {
                xInput = 0;
            }
        }

        /// <Summary> 
        /// Disable unnecessary components for main menu preview.
        /// Should be called before the Start() function.
        ///</Summary>
        public void SetAsPreview(){
            if (forPreview)
            {
                forPreview = true;

                invulnerabilityIndicator.SetActive(false);
                movementController.DestroyRigidbody();
                ai.enabled = false;
                meleeWeapon.enabled = false;
                Destroy(photonView);

                // Get the chosen character (locally):
                for (int i = 0; i < characters.Length; i++)
                {
                    if (characters[i].data == DataCarrier.characters[DataCarrier.chosenCharacter])
                    {
                        curCharacterID = i;
                    }
                }

                // Enable only the chosen character's graphics:
                for (int i = 0; i < characters.Length; i++)
                {
                    characters[i].animator.gameObject.SetActive(i == curCharacterID);
                }
                return;
            }
        }

        public void EquipStartingWeapon()
        {
            if (character.data.startingWeapon)
            {
                // A negative value as a weapon id is invalid, but we can use it to tell everyone that it's a starting weapon since starting weapons don't need id's because 
                // there is only one starting weapon for each character anyway.
                // Starting weapons might not be set as spawnable in a map so refer to the current character's data instead:
                lastWeaponId = -(curCharacterID + 1); // deacreased by 1 because an index of 0 will not do the trick (will be resolve later)

                // Spawn the starting weapon:
                originalWeapon = character.data.startingWeapon;
                curWeapon = Instantiate(originalWeapon, weaponHandler);
                curWeapon.owner = this;
            }
        }

        public void RestartPlayer()
        {
            // Get the dedicated player instance for this player:
            playerInstance = gm.GetPlayerInstance(isBot? ai.botID : photonView.Owner.ActorNumber);

            // Subscibe to Controls Manager's jump event if player is ours:
            if (isPlayerOurs){
                gm.controlsManager.jump += Jump;
            }

            // Get the chosen character of this player (we only need the index of the chosen character in DataCarrier's characters array):
            int chosenCharacter = playerInstance.character;
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].data == DataCarrier.characters[chosenCharacter])
                {
                    curCharacterID = i;
                }
            }

            // Enable only the chosen character's graphics:
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i].animator.gameObject.SetActive(i == curCharacterID);
            }

            // Get the stat infos from the character data:
            health = character.data.maxHealth;

            // Remove any weapon:
            DisarmItem();
        }

        public void Jump()
        {
            if (!gm.gameStarted || gm.isGameOver) return;

            if (!isOnJumpPad && movementController.isGrounded && movementController.allowJump)
            {
                // Call jump in character controller:
                movementController.Jump();

                if (character.data.jumpSFX.Length > 0)
                {
                    aus.PlayOneShot(character.data.jumpSFX[Random.Range(0, character.data.jumpSFX.Length)]);
                }
            }
        }
        
        public void Land()
        {
            gm.pooler.Spawn("LandDust", transform.position);
            // Sound:
            if (character.data.landingsSFX.Length > 0) aus.PlayOneShot(character.data.landingsSFX[Random.Range(0, character.data.landingsSFX.Length)]);
        }

        public void OwnerShootCommand(){
            photonView.RPC("Shoot", RpcTarget.All, mousePos, movementController.position, movementController.velocity);
        }
        // Called by the owner from mobile or pc input:
        public void OwnerMeleeAttack()
        {
            if (curMeleeAttackRate >= 1)
            {
                photonView.RPC("MeleeAttack", RpcTarget.All);
                curMeleeAttackRate = 0;
            }
        }
        public void OwnerThrowGrenade()
        {
            if (curGrenadeCount > 0)
            {
                curGrenadeCount -= 1;
                photonView.RPC("ThrowGrenade", RpcTarget.All);
            }
        }

        void Die()
        {
            if (!gm.isGameOver)
            {
                // Multikill (if we are the killer and we are not the one dying):
                PlayerController killerPc = gm.GetPlayerControllerOfPlayer(lastDamageDealer);

                // If killer matched:
                if (killerPc)
                {
                    // If the killer is ours (bots are also ours if we're the master client):
                    if (killerPc.playerInstance.playerID != playerInstance.playerID && (killerPc.isPlayerOurs || (PhotonNetwork.IsMasterClient && killerPc.isBot)))
                    {
                        killerPc.curMultikill += 1;
                        killerPc.curMultikillDelay = gm.multikillDuration;

                        // Add a bonus score to killer for doing a multi kill:
                        if (killerPc.curMultikill > 1)
                        {
                            int scoreToAdd = gm.multiKillMessages[Mathf.Clamp(killerPc.curMultikill - 1, 0, gm.multiKillMessages.Length - 1)].bonusScore;
                            gm.AddScore(lastDamageDealer, false, false, scoreToAdd);
                        }
                    }
                }

                // Let GameManager handle the other death related stuff (scoring, display kill/death message etc...):
                gm.SomeoneDied(playerInstance.playerID, lastDamageDealer.playerID);

                // and then destroy (give a time for the death animation):
                if (photonView.IsMine)
                {
                    Invoke("PhotonDestroy", 1f);
                }
            }

            // Cancel any movement:
            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = false;
            }
            // and remove the rigidbody:
            movementController.DestroyRigidbody();
            // ...and others:
            invulnerabilityIndicator.SetActive(false);
        }

        public void Teleport(Vector3 newPos)
        {
            networkPos = newPos;
            if (movementController) movementController.transform.position = networkPos;
        }

        // Instant death from dead zone:
        public void DeadZoned()
        {
            lastDamageDealer = playerInstance;
            health = 0;

            // VFX:
            if (gm.maps[gm.chosenMap].deadZoneVFX)
            {
                Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
            }
        }

        void PhotonDestroy()
        {
            PhotonNetwork.Destroy(photonView);
        }


        /// <summary>
        /// Deal damage to player.
        /// </summary>
        /// <param name="fromPlayer">Damage dealer player name.</param>
        /// <param name="value">Can be either a weapon id (if a gun was used) or a damage value (if melee attack or grenade).</param>
        /// <param name="gun">If set to <c>true</c>, "value" will be used as weapon id.</param>
        public void ApplyDamage(int fromPlayer, int value, bool gun){
            photonView.RPC("Hurt", RpcTarget.AllBuffered, fromPlayer, value, gun);
        }
        [PunRPC]
        void Hurt(int fromPlayer, int value, bool gun)
        {
            if (!gm.isGameOver)
            {
                // Only damage if vulnerable:
                if (!invulnerable)
                {
                    int finalDamage = 0; // the damage value

                    // If damage is from a gun:
                    if (gun)
                    {
                        // Get the weapon used using the "value" parameter as weapon id (or if it's a negative value, then it's a character id):
                        Weapon weaponUsed = value >= 0 ? gm.maps[gm.chosenMap].spawnableWeapons[value] : characters[value * -1 - 1].data.startingWeapon;

                        // ...then get the weapon's damage value:
                        finalDamage = weaponUsed.damage;
                    }
                    else
                    {
                        // If not a gun then it could be from a grenade or a melee attack, either way, just assume that the "value" parameter is the damage value:
                        finalDamage = value;
                    }

                    // Now do the damage application:
                    // First, calculate the penetrating damage:
                    int damageToHP = finalDamage - shield;
                    // ...then apply damage to shield:
                    shield = shield - finalDamage <= 0? 0 : shield - finalDamage;
                    // Finally, apply the excess damage to HP:
                    if (damageToHP > 0) health -= damageToHP;

                    // Let others know our real health value if this player is ours:
                    if (photonView.IsMine){

                    }
                    // Do a damage prediction locally while waiting for the server's side if not ours:
                    else{

                    }

                    // Damage popup:
                    if (gm.damagePopups)
                    {
                        gm.pooler.Spawn("DamagePopup", weaponHandler.position).GetComponent<DamagePopup>().Set(finalDamage);
                    }

                    // Sound:
                    aus.PlayOneShot(hurtSFX[Random.Range(0, hurtSFX.Length)]);

                    // Do the "hurt screen" effect:
                    if (isPlayerOurs)
                    {
                        gm.ui.Hurt();
                    }
                    lastDamageDealer = gm.GetPlayerInstance(fromPlayer);
                }
            }
        }
        [PunRPC]
        void TriggerDeadZone(Vector2 position){
            movementController.position = position;
            networkPos = position;
            DeadZoned();
        }
        // Called by the owner client of this player:
        [PunRPC]
        public void Shoot(Vector3 curMousePos, Vector2 curPlayerPos, Vector2 curVelocity)
        {
            // Set updated position and aim directly so everything's synced up on shoot:
            mousePos = curMousePos;
            nMousePos = curMousePos;
            if (movementController) movementController.position = curPlayerPos;
            networkPos = curPlayerPos;
            movementController.velocity = curVelocity;
            // ...then the shooting itself:
            curWeapon.Shoot();
        }
        [PunRPC]
        public void ThrowGrenade()
        {
            // Sound:
            aus.PlayOneShot(throwGrenadeSFX);

            // Grenade spawning:
            if (photonView.IsMine)
            {
                Vector2 p1 = new Vector2(grenadePoint.position.x, grenadePoint.position.y);
                Vector2 p2 = new Vector2(weaponHandler.position.x, weaponHandler.position.y);
                object[] data = new object[] { (p1 - p2) * grenadeThrowForce, playerInstance.playerID }; // the instantiation data of a grenade includes the direction of the throw and the owner's player ID 

                PhotonNetwork.Instantiate(grenadePrefab, grenadePoint.position, Quaternion.identity, 0, data);
            }
        }
        [PunRPC]
        public void MeleeAttack()
        {
            meleeWeapon.Attack(photonView.IsMine, this);
        }
        [PunRPC]
        public void GrabWeapon(int id, int getFromSpawnPoint)
        {
            // Find the weapon in spawnable weapons of the current map:
            Weapon theWeapon = getFromSpawnPoint != -1 ? gm.maps[gm.chosenMap].weaponSpawnPoints[getFromSpawnPoint].onlySpawnThisHere : gm.maps[gm.chosenMap].spawnableWeapons[id];

            // Disarm current item first (if we have one):
            DisarmItem();

            originalWeapon = theWeapon;
            // ...then instantiate one based on the new item:
            curWeapon = Instantiate(theWeapon, weaponHandler);
            curWeapon.owner = this;
            // Also, let's save the weapon ID:
            lastWeaponId = getFromSpawnPoint != -1 ? System.Array.IndexOf(gm.maps[gm.chosenMap].spawnableWeapons, gm.maps[gm.chosenMap].weaponSpawnPoints[getFromSpawnPoint].onlySpawnThisHere) : id;
        }
        [PunRPC]
        public void ReceivePowerUp(int id, int getFromSpawnPoint)
        {
            // Find the power-up in spawnable power-ups of the current map:
            PowerUp thePowerUp = getFromSpawnPoint != -1 ? gm.maps[gm.chosenMap].powerUpSpawnPoints[getFromSpawnPoint].onlySpawnThisHere : gm.maps[gm.chosenMap].spawnablePowerUps[id];

            // ...then do the power-up's effects:
            // HEALTH:
            if (thePowerUp.fullRefillHealth)
            {
                health = character.data.maxHealth;
            }
            else{
                health += thePowerUp.addedHealth;
                health = Mathf.Clamp(health, 0, character.data.maxHealth);
            }
            // SHIELD:
            if (thePowerUp.fullRefillShield)
            {
                shield = maxShield;
            }
            else{
                shield += thePowerUp.addedShield;
                shield = Mathf.Clamp(shield, 0, maxShield);
            }
            
            // ADD GRENADE:
            curGrenadeCount += thePowerUp.addedGrenade;
            // AMMO REFILL:
            if (curWeapon && thePowerUp.fullRefillAmmo) curWeapon.curAmmo = curWeapon.ammo;

            // Update others about our current vital stats (health and shield):
            photonView.RPC("UpdateOthers", RpcTarget.Others, health, shield);
        }

        [PunRPC]
        public void Emote(int emote){
            if (curEmote && curEmote.isReady){
                curEmote.Show(emote);
            }
        }
        // *************************************************

        [PunRPC]
        public void UpdateOthers(int curHealth, int curShield)
        {
            health = curHealth;
            shield = curShield;
        }

        void DisarmItem()
        {
            if (curWeapon)
            {
                //Pickup p = Instantiate (pickupPrefab, transform.up * 2, Quaternion.identity);
                //p.itemHandled = originalItem;
                Destroy(curWeapon.gameObject);
            }
        }

        void OnDestroy()
        {
            if (!forPreview) gm.playerControllers.RemoveAll(p => p == null);
        }
        // *****************************************************

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (forPreview) return;

            if (stream.IsWriting && photonView.IsMine)
            {
                // Send controls over network
                stream.SendNext(movementController ? movementController.position : new Vector2());  // position
                stream.SendNext(movementController ? movementController.velocity : new Vector2());  // velocity
                stream.SendNext((Vector2)mousePos);                                                 // send a Vector2 version of the mouse position because we don't need the Z axis
                stream.SendNext(moving);
                stream.SendNext(isFalling);
                stream.SendNext(xInput);
            }
            else if (stream.IsReading)
            {

                // Receive controls
                networkPos = (Vector2)(stream.ReceiveNext());
                movementController.velocity = (Vector2)(stream.ReceiveNext());
                nMousePos = (Vector2)(stream.ReceiveNext());
                moving = (bool)(stream.ReceiveNext());
                isFalling = (bool)(stream.ReceiveNext());
                xInput = (float)(stream.ReceiveNext());
                
                // Calculate lag:
                lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                
                // If is still moving, do predict next location based on current velocity and lag:
                if (Helper.GetDistance(lastPos, networkPos) > 0.2f)
                {
                    networkPos += (movementController.velocity * lag);
                }
                lastPos = networkPos;

                // If network position is just too far, force to update local position:
                if (Helper.GetDistance(networkPos, transform.position) > 0.2f){
                    movementController.position = networkPos;
                }
            }
        }
    }
}