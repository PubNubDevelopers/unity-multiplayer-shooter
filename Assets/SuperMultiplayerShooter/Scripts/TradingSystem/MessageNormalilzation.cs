using Newtonsoft.Json;
using PubNubUnityShowcase;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageNormalilzation : MonoBehaviour
{
    public const string TYPE_KEY = "payload_type";
    public const string COMMAND_KEY = "string";

    public static Dictionary<string, object> GetCommandMeta()
    {
        return new Dictionary<string, object>
            {
                { TYPE_KEY, COMMAND_KEY }
            };
    }

    public static Dictionary<string, object> GetMeta<T>() where T : struct, IJsonSerializable
    {
        return new Dictionary<string, object>
            {
                { TYPE_KEY, $"{typeof(T).Name}" }
            };
    }

    public static T GetPayload<T>(object obj) where T : struct, IJsonSerializable
    {
        try
        {
            T payload = (T)JsonConvert.DeserializeObject(obj.ToString(), typeof(T));
            return payload;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return default;
        }
    }
}
