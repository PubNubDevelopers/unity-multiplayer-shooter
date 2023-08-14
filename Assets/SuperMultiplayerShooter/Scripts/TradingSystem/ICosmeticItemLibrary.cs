using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    //NOTE: its implemented as interface so that later it would be easier to refactor it for using Addressables

    /// <summary>
    /// Library to get cosmetic items data
    /// </summary>
    public interface ICosmeticItemLibrary
    {
        CosmeticItem GetCosmeticItem(int index);
    }
}