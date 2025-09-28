using System;
using System.Collections.Generic;
using System.Linq;
using Features.CardMatching.Scripts.Data;
using Features.CardMatching.Scripts.Declaration;
using Naninovel;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Features.CardMatching.Scripts.Implementation
{
    [InitializeAtRuntime]
    public class CardMatchingService : ICardMatchingService
    {
        private const string CARD_GAME_COMPLETED_VAR = "cardGameCompleted";

        public event Action<CardData, CardData, bool> OnCardsMatched;
        public event Action OnGameCompleted;

        private CardMatchingState _state = CardMatchingState.WaitingForFirstCard;
        private CardData _firstSelected;
        private CardData _secondSelected;
        private List<CardData> _cards;
        private CustomVariableManager _customVariableManager;

        public CardMatchingConfig Configuration { get; private set; }
        public bool IsGameCompleted { get; private set; }

        public UniTask InitializeServiceAsync()
        {
            Configuration = Engine.GetConfiguration<CardMatchingConfig>();
            _customVariableManager = Engine.GetService<CustomVariableManager>();
            IsGameCompleted = GetBoolVariable(CARD_GAME_COMPLETED_VAR);

            Debug.Log("[CardMatchingService] Initialized successfully.");

            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            _firstSelected = null;
            _secondSelected = null;
            _cards?.Clear();
            _state = CardMatchingState.WaitingForFirstCard;

            IsGameCompleted = false;
            SyncToNaninovel();
        }

        public void DestroyService()
        {
            ResetService();
        }

        public void StartGame(int pairCount)
        {
            ResetService();

            _cards = GenerateCards(pairCount);
            _state = CardMatchingState.WaitingForFirstCard;
        }

        public void SelectCard(CardData card)
        {
            if (card.IsMatched)
            {
                return;
            }

            if (_state == CardMatchingState.AnimatingUnmatch || _state == CardMatchingState.GameFinished)
            {
                return;
            }

            if (card == _firstSelected)
            {
                return;
            }

            if (_state == CardMatchingState.WaitingForFirstCard)
            {
                _firstSelected = card;
                _state = CardMatchingState.WaitingForSecondCard;
            }
            else if (_state == CardMatchingState.WaitingForSecondCard)
            {
                _secondSelected = card;
                _state = CardMatchingState.CheckingMatch;

                CheckMatch();
            }
        }

        private void CheckMatch()
        {
            var isMatch = _firstSelected.PairId == _secondSelected.PairId;

            if (isMatch)
            {
                _firstSelected.IsMatched = true;
                _secondSelected.IsMatched = true;

                OnCardsMatched?.Invoke(_firstSelected, _secondSelected, true);

                ResetSelection();

                if (IsGameFinished())
                {
                    _state = CardMatchingState.GameFinished;

                    IsGameCompleted = true;
                    SyncToNaninovel();

                    OnGameCompleted?.Invoke();
                    Debug.Log("[CardMatchingService] Game completed and synced to Naninovel!");
                }
                else
                {
                    _state = CardMatchingState.WaitingForFirstCard;
                }
            }
            else
            {
                _state = CardMatchingState.AnimatingUnmatch;
                OnCardsMatched?.Invoke(_firstSelected, _secondSelected, false);
            }
        }

        public void CompleteAnimation()
        {
            if (_state == CardMatchingState.AnimatingUnmatch)
            {
                ResetSelection();
                _state = CardMatchingState.WaitingForFirstCard;
            }
        }

        private bool GetBoolVariable(string name)
        {
            return bool.TryParse(_customVariableManager.GetVariableValue(name), out var result) && result;
        }

        private void SyncToNaninovel()
        {
            _customVariableManager.SetVariableValue(CARD_GAME_COMPLETED_VAR, IsGameCompleted.ToString().ToLower());
        }

        private void ResetSelection()
        {
            _firstSelected = null;
            _secondSelected = null;
        }

        private bool IsGameFinished()
        {
            return _cards.All(card => card.IsMatched);
        }

        private List<CardData> GenerateCards(int pairCount)
        {
            var result = new List<CardData>();

            for (var i = 0; i < pairCount; i++)
            {
                result.Add(new CardData(i));
                result.Add(new CardData(i));
            }

            for (var i = result.Count - 1; i > 0; i--)
            {
                var randomIndex = Random.Range(0, i + 1);
                (result[i], result[randomIndex]) = (result[randomIndex], result[i]);
            }

            return result;
        }

        public List<CardData> GetCards()
        {
            return _cards;
        }

        public CardMatchingState GetCurrentState()
        {
            return _state;
        }
    }
}
