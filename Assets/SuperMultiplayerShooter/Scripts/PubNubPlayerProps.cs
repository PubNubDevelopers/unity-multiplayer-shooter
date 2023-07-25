using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{

    public class PubNubPlayerProps : MonoBehaviour
    {
        public bool IsBot { get; set; }
        public int OwnerId { get; set; }
        public bool IsMine { get; set; }
        public int BotId { get; set;  }
        public bool IsPreview { get; set; }
    }
}