namespace Features.CardMatching.Scripts.Data
{
    public enum CardMatchingState
    {
        WaitingForFirstCard,
        WaitingForSecondCard,
        CheckingMatch,
        AnimatingUnmatch,
        GameFinished
    }
}
