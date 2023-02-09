using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace Visyde{

    /// <summary>
    /// Loading Screen Manager
    /// - A simple loading screen manager that displays load progress.
    /// </summary>

    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("References:")]
        public Slider loadingBar;

        void Start()
        {
            PhotonNetwork.LoadLevel(DataCarrier.sceneToLoad);
        }

        void Update()
        {
            loadingBar.value = PhotonNetwork.LevelLoadingProgress;
        }
    }
}