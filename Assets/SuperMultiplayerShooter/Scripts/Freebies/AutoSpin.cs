using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Auto Spin
    /// - A very simple script for endless spinning
    /// </summary>
	
    public class AutoSpin : MonoBehaviour
    {

        public Vector3 speed;

        // Update is called once per frame
        void Update()
        {
            if (speed.magnitude > 0)
            {
                transform.localEulerAngles += (speed * Time.deltaTime);
            }
        }
    }
}