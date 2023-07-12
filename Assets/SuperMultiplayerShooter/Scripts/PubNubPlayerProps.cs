using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{

    public class PubNubPlayerProps : MonoBehaviour
    {
        //  DCC todo sort out the capitalization
        public bool isBot { get; set; }
        public int ownerId { get; set; }
        public bool IsMine { get; set; }
        public int botId { get; set;  }
        public bool preview { get; set; }
    }
}