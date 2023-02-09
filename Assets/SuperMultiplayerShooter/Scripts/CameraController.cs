using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Camera Controller
    /// - controls the in-game camera that follows the player.
    /// </summary>

    [ExecuteInEditMode]
    public class CameraController : MonoBehaviour
    {
        [Header("Settings:")]
        public float xFollowSpeed = 7;
        public float yFollowSpeed = 7;
        public Vector2 followOffset;
        [Space]
        public float normalZoom;
        public float noPlayerZoom;
        public float zoomChangeFactor;
        public float zoomSpeed;
        public float shakeDamping = 1f;

        [Space]
        [Header("References:")]
        public Camera theCamera;

        // Internal:
        public PlayerController target;
        [HideInInspector] public GameManager gm;
        float zPos;
        Vector3 curPos;
        float camWidth;
        float camHeight;
        Vector3 lastPlayerPos;                              // for death cam
        Vector3 lastMousePos;                               // for death cam
        Vector3 initCamPos;                                 // for cam shake
        float shakeAmount;
        float curShakeDur;

        void OnEnable()
        {
            // Save the initial position of the main camera for the cam shake:
            initCamPos = theCamera.transform.localPosition;
        }

        // Use this for initialization
        void Start()
        {
            // Initial Z position:
            zPos = transform.position.z;
        }

        void LateUpdate()
        {
            if (target)
            {
                // Forget target if dead:
                if (target.isDead)
                {
                    target = null;
                    return;
                }

                lastPlayerPos = target.transform.position;
                lastMousePos = target.mousePos;
            }

            // Get target and mouse position:
            Vector3 targetPos = target ? target.transform.position : lastPlayerPos;
            Vector3 targetMousePos = target ? gm.gameStarted ? target.mousePos : target.transform.position : lastMousePos;

            // Get camera view's width and height:
            camHeight = theCamera.orthographicSize * 2;
            camWidth = camHeight * theCamera.aspect;

            // Zoom amount:
            float finalZoom = noPlayerZoom;
            if (target)
            {
                if (gm.gameStarted)
                {
                    finalZoom = normalZoom + (zoomChangeFactor * target.movementController.velocity.magnitude) + (target.curWeapon ? target.curWeapon.sightRange : 0f) * 0.4f;
                }
                else
                {
                    finalZoom = normalZoom;
                }
            }
            theCamera.orthographicSize = Mathf.Lerp(theCamera.orthographicSize, gm.countdownStarted ? finalZoom : noPlayerZoom, Time.deltaTime * zoomSpeed);

            // Do cam shake:
            if (curShakeDur > 0)
            {
                curShakeDur -= Time.deltaTime * shakeDamping;
                // Generate a random position:
                Vector3 randomShake = initCamPos + Random.insideUnitSphere * shakeAmount;
                randomShake.z = 0;

                // Move to that position:
                theCamera.transform.localPosition = Vector3.Lerp(theCamera.transform.localPosition, randomShake, Time.deltaTime * shakeAmount * 40f);
            }
            else
            {
                theCamera.transform.localPosition = initCamPos;
            }

            // Camera movement:
            if (gm.countdownStarted)
            {
                // *How far the player can see is determined by the 'sightRange' variable of the weapon:
                float sR = 1;
                if (target)
                {
                    sR = (target.curWeapon ? target.curWeapon.sightRange : 1f);
                }
                Vector3 finalPos = targetPos + (targetMousePos - targetPos).normalized * sR + new Vector3(followOffset.x, followOffset.y);
                curPos.x = Mathf.Lerp(curPos.x, finalPos.x, Time.deltaTime * xFollowSpeed);
                curPos.y = Mathf.Lerp(curPos.y, finalPos.y, Time.deltaTime * yFollowSpeed);
                curPos.z = zPos;
                transform.position = curPos;
            }
            
            // Restrict camera inside an active map's bounds:
            GameMap map = gm.getActiveMap;
            if (map)
            {
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, map.boundOffset.x - (map.bounds.x - camWidth / 2), map.boundOffset.x + (map.bounds.x - camWidth / 2)), Mathf.Clamp(transform.position.y, map.boundOffset.y - (map.bounds.y - camHeight / 2), map.boundOffset.y + (map.bounds.y - camHeight / 2)), zPos);
            }
        }

        // Call to do a cam shake:
        public void DoShake(float amount, float duration)
        {
            shakeAmount = amount;
            curShakeDur = duration;
        }
    }
}
