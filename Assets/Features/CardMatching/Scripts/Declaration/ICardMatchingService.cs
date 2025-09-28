using System;
using System.Collections.Generic;
using Features.CardMatching.Scripts.Data;
using Naninovel;

namespace Features.CardMatching.Scripts.Declaration
{
    public interface ICardMatchingService : IEngineService<CardMatchingConfig>
    {
        event Action<CardData, CardData, bool> OnCardsMatched;
        event Action OnGameCompleted;

        void StartGame(int pairCount);
        void SelectCard(CardData card);
        void CompleteAnimation();
        List<CardData> GetCards();
        CardMatchingState GetCurrentState();
    }
}
