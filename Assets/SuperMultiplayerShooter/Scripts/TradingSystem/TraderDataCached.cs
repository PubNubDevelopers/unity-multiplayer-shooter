using System.Threading.Tasks;
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

            return new TraderData(
                metadata.Uuid,
                metadata.Name,
                DataCarrier.chosenCharacter,
                inventory,
                inventory.CosmeticItems[0]); //TODO: find a way to get this 
        }
    }
}