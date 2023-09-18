using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{
    public interface IJsonSerializable
    {
        string RawJson => JsonConvert.SerializeObject(this);
    }
}