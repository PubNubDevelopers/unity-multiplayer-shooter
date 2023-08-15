using System.Threading.Tasks;

namespace PubNubUnityShowcase
{
    public interface ITradingDatastore
    {
        Task<TraderData> GetTraderData(string traderID);
    }
}