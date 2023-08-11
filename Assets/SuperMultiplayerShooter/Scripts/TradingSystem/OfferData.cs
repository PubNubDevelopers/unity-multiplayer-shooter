using Unity.VisualScripting;
using UnityEngine;

namespace PubNubUnityShowcase
{
    [System.Serializable]
    public struct OfferData : IJsonSerializable
    {
        [SerializeField] private int initiatorGives;
        [SerializeField] private int initiatorReceives;
        [SerializeField] private bool final;
        [SerializeField] private TradeSessionData.Role target;
        [SerializeField] private int version;
        [SerializeField] private OfferState state;

        /// <summary>
        /// Initial Offer
        /// </summary>
        private OfferData(TradeSessionData.Role target, int initiatorGives, int initiatorReceives, bool final, OfferState state, int version)
        {
            this.target = target;
            this.initiatorGives = initiatorGives;
            this.initiatorReceives = initiatorReceives;            
            this.final = final;
            this.version = version;
            this.state = state;
        }

        public TradeSessionData.Role Target { get => target; set => target = value; }
        public int InitiatorGives { get => initiatorGives; set => initiatorGives = value; }
        public int InitiatorReceives { get => initiatorReceives; set => initiatorReceives = value; }
        public bool FinalOffer { get => final; set => final = value; }        
        public int Version { get => version; set => version = value; }
        public OfferState State { get => state; set => state = value; }

        public static OfferData GenerateInitialOffer(int initiatorGives, int initiatorReceives, bool final)
        {
            return new OfferData(
                TradeSessionData.Role.Initiator,
                initiatorGives,
                initiatorReceives,
                final,
                OfferState.open,
                0);
        }

        public static OfferData GetAcceptedOffer(OfferData original)
        {
            //swap targets
            var target = original.Target == TradeSessionData.Role.Initiator ? TradeSessionData.Role.Initiator : TradeSessionData.Role.Respondent;

            return new OfferData(
                target,
                original.InitiatorGives,
                original.InitiatorReceives,
                true,
                OfferState.accepted,
                original.version);
        }

        public static OfferData GetRejectedOffer(OfferData original)
        {
            //swap targets
            var target = original.Target == TradeSessionData.Role.Initiator ? TradeSessionData.Role.Initiator : TradeSessionData.Role.Respondent;

            return new OfferData(
                target,
                original.InitiatorGives,
                original.InitiatorReceives,
                true,
                OfferState.rejected,
                original.version);
        }

        public static OfferData GenerateCounterlOffer(OfferData original, int initiatorGives, int initiatorReceives, bool final)
        {
            //swap targets
            var target = original.Target == TradeSessionData.Role.Initiator ? TradeSessionData.Role.Initiator : TradeSessionData.Role.Respondent;

            //add counter

            return new OfferData(
                target,
                initiatorGives,
                initiatorReceives,
                final,
                OfferState.open,
                original.version += 1);
        }

        public enum OfferState
        {
            open, 
            accepted,
            rejected
        }

        //public override bool Equals(object obj)
        //{
        //    return obj is OfferData data &&
        //           initiatorGives == data.initiatorGives &&
        //           initiatorReceives == data.initiatorReceives;
        //}
    }
}