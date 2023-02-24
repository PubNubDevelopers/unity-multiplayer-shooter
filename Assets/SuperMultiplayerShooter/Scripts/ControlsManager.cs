using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Visyde
{
    /// <summary>
    /// Controls Manager
    /// - manages player control inputs for different platforms (PC and mobile) in one place.
    /// </summary>

    public class ControlsManager : MonoBehaviour
    {
        [HideInInspector] public bool mobileControls;
        [Header("References:")]
        public Joystick moveStick;
        public Joystick shootStick;

		[Tooltip("The extent of the shootStick where shooting begins.")]
		[Range(0.1f, 1f)]
		public float shootingThreshold;

        [Header("Jumping using the Move Stick:")]
        public bool enableMoveStickJumping;
        [Tooltip("The Y value of moveStick where jumping begins.")]
        [Range(0.1f, 1f)]
        public float jumpingYStart;
        [Tooltip("The extent of the moveStick where jumping begins.")]
        [Range(0.1f, 1f)]
        public float jumpingThreshold;

        // Movement:
        [HideInInspector]
        public float horizontal;
        [HideInInspector]
        public float vertical;
        [HideInInspector]
        public float horizontalRaw;
        [HideInInspector]
        public float verticalRaw;
        public UnityAction jump;

        // Shooting:
        [HideInInspector]
        public bool shoot;              
        [HideInInspector]
        public float aimX;
        [HideInInspector]
        public float aimY;

        // UI:
        [HideInInspector]
        public bool showScoreboard;

        // Update is called once per frame
        void Update()
        {
            // Movement input:
            float x = moveStick.xValue;
            float y = moveStick.yValue;
            horizontal = mobileControls ? x : Input.GetAxis("Horizontal");
            vertical = mobileControls ? y : Input.GetAxis("Vertical");
            horizontalRaw = mobileControls ? x == 0 ? 0 : x > 0 ? 1 : -1 : Input.GetAxisRaw("Horizontal");
            verticalRaw = mobileControls ? y == 0 ? 0 : y > 0 ? 1 : -1 : Input.GetAxisRaw("Vertical");

            // Jumping (Mobile controls have 2 options, either use move stick as a jumping control when the 
            // Y axis is over the 'jumpingYStart' and 'jumpingThreshold', or simply use an on-screen button): 
            if (mobileControls){
                if (enableMoveStickJumping && (y >= jumpingYStart && moveStick.progress >= jumpingThreshold)) Jump();
            }
            else{
                if (Input.GetButton("Jump")) Jump();
            }

            // Shooting input:
            aimX = shootStick.xValue;
            aimY = shootStick.yValue;

            // Show/hide scoreboard for PC:
            if (!mobileControls)
            {
                showScoreboard = Input.GetKey(KeyCode.Tab);
            }
        }
        void LateUpdate(){
            shoot = mobileControls? shootStick.progress >= shootingThreshold && shootStick.isHolding : Input.GetButton("Fire1");
        }

        // Jumping (can be called by an on-screen button):
        public void Jump()
        {
            if (jump != null)
            jump.Invoke();
        }

        // Holding a button to show/hide the scoreboard.
        public void ShowScoreBoard(bool show)
        {
            showScoreboard = show;
        }
    }
}
