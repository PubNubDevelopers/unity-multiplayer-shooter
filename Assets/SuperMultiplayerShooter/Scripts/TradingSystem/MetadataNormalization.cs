using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    public static class MetadataNormalization
    {
        /// <param name="customData">This is AppContext's UserMetadata.Custom </param>
        public static List<int> GetHats(Dictionary<string, object> customData)
        {
            List<int> availableHats = new List<int>();

            if (customData != null)
            {
                if (customData.ContainsKey("hats"))
                {
                    availableHats = JsonConvert.DeserializeObject<List<int>>(customData["hats"].ToString());
                }
            }

            return availableHats;
        }
        

        /// <param name="customMetadata">This is AppContext's UserMetadata.Custom </param>
        public static void ReplaceHats(string userId, Dictionary<string, object> customMetadata, int existing, int newHat)
        {
            string old = JsonConvert.SerializeObject(customMetadata);

            List<int> currentInventory = GetHats(customMetadata);
            List<int> updatedInventory = currentInventory.Select(hat => hat == existing ? newHat : hat).ToList();         
            //note: hats' keyvalue is seriliazed as <string,string> not as <string,List<int>> !!!
            string listAsJson = JsonConvert.SerializeObject(updatedInventory);
            customMetadata["hats"] = listAsJson;
                       
            //Update current inventory.
            if(userId.Equals(PNManager.pubnubInstance.pubnub.GetCurrentUserId()))
            {
                //If the hat that is being traded away is currently equipped, equip the new hat that was just tradded to the user.
                if (DataCarrier.chosenHat == existing)
                {
                    DataCarrier.chosenHat = newHat;
                    customMetadata["chosen_hat"] = DataCarrier.chosenHat;
                }

                Connector.instance.UpdateAvailableHats(updatedInventory);
            }

            //For any player, update the CachedPlayersList with the new updated inventory
            PNManager.pubnubInstance.CachedPlayers[userId].Custom = customMetadata;
        }
    }
}