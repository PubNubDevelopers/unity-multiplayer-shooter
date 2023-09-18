using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Visyde;

namespace PubNubUnityShowcase
{
    public class TraderDataCached : ITradingDatastore
    {
        async Task<TraderData> ITradingDatastore.GetTraderData(string traderID)
        {
            TradeInventoryData inventory = TradeInventoryData.GetEmpty();
            if (PNManager.pubnubInstance.CachedPlayers.TryGetValue(traderID, out var metadata))
                inventory = new TradeInventoryData(MetadataNormalization.GetHats(metadata.Custom));

            await Task.CompletedTask;

            int chosenCharacter = 0;
            if (metadata.Custom.ContainsKey("chosen_character"))
            {
                chosenCharacter = Int32.Parse(metadata.Custom["chosen_character"].ToString());
            }
            //Legacy Default situations
            else
            {
                metadata.Custom.Add("chosen_character", 0); // Defaults to the first character.
                PNManager.pubnubInstance.CachedPlayers[traderID].Custom = metadata.Custom;
            }
            return new TraderData(
                traderID,
                metadata.Name,
                chosenCharacter,
                inventory,
                inventory.CosmeticItems[0]); // using the first hat in the player's inventory, not their selected hat.
        }
    }
}