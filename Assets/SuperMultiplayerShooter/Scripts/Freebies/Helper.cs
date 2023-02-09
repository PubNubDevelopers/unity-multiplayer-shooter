using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Helper
    /// - A simple helper library to make things simpler and easier
    /// </summary>

    public class Helper
    {
        public static float GetDistance(Vector3 pos1, Vector3 pos2)
        {
            Vector3 p;
            float distanceSquared;

            p.x = pos2.x - pos1.x;
            p.y = pos2.y - pos1.y;
            p.z = pos2.z - pos1.z;

            distanceSquared = p.x * p.x + p.y * p.y + p.z * p.z;
            return Mathf.Sqrt(distanceSquared);
        }

        public static void LogError(Component from, string message){
            Debug.LogError("<color=magenta>(" + from.GetType() + ")</color>" + " <color=red>" + message + "</color>");
        }
    }
}