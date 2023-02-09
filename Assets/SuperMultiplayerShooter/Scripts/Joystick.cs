using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace  Visyde
{
    /// <summary>
    /// Joystick
    /// - A simple joystick script. You can replace this with your own favorite joystick script if you want.
    /// </summary>

    public class Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Settings:")]
        public float restriction;
        public bool snapsBack;

        [Header("References:")]
        public RectTransform touchArea;
        public Transform stick;

        [HideInInspector] public float xValue;
        [HideInInspector] public float yValue;
        [HideInInspector] public float progress;
        [HideInInspector] public bool isHolding;

        // Internals:
        Vector2 inputPos;

        // Use this for initialization
        void Start()
        {
            // Reset position:
            stick.localPosition = Vector3.zero;
        }

        // Update is called once per frame
        void Update()
        {
            // The final joystick position variable:
            Vector3 finalPos = inputPos;

            // Set the finalPos to the touch position:
            if (isHolding){
                finalPos = inputPos;
            }
            else{
                // Reset joystick position:
                if (snapsBack)
                {
                    finalPos = Vector3.zero;
                }
            }

            // Restrict position:
            finalPos = Vector3.ClampMagnitude(finalPos, restriction);
            stick.localPosition = finalPos;

            // Output:
            xValue = stick.localPosition.x / restriction;
            yValue = stick.localPosition.y / restriction;
            progress = finalPos.magnitude / restriction;
        }

        public void Control(PointerEventData e){
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(touchArea, e.position, e.pressEventCamera, out inputPos)) { }
        }

        public void OnDrag(PointerEventData e){
            Control(e);
            isHolding = true;
        }
        public void OnPointerDown(PointerEventData e)
        {
            Control(e);
            isHolding = true;
        }
        public void OnPointerUp(PointerEventData e)
        {
            isHolding = false;
        }
    }
}
