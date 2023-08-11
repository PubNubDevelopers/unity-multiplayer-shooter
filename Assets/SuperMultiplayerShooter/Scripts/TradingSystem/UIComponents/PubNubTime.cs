using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase {
    public class PubNubTime : MonoBehaviour
    {

        [SerializeField] private double timeToken;
        [SerializeField] private string timeUTC;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            timeUTC = UnixTimeStampToDateTime(timeToken).ToString();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}