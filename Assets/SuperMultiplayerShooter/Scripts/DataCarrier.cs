using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Visyde
{
    /// <summary>
    /// Data Carrier
    /// - stores data for use between scenes. Also, this handles switching between scenes.
    /// </summary>

    public class DataCarrier
    {
        public static string message = "";
        public static string sceneToLoad = "";
        public static int chosenCharacter;
        public static CharacterData[] characters;

        public static int chosenHat = -1;

        public static int coins = 0;

        public static void LoadScene(string sceneName)
        {
            sceneToLoad = sceneName;
            SceneManager.LoadScene("LoadingScreen");
        }
    }
}