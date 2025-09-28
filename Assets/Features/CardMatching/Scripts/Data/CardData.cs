namespace Features.CardMatching.Scripts.Data
{
    public class CardData
    {
        public int PairId { get; }
        public bool IsMatched { get; set; }

        public CardData(int pairId)
        {
            PairId = pairId;
            IsMatched = false;
        }
    }
}
