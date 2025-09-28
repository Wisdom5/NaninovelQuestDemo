using System.Collections;
using Features.CardMatching.Scripts.Data;
using Features.CardMatching.Scripts.Declaration;
using Naninovel;
using UnityEngine;

namespace Features.CardMatching.Scripts.Presentation
{
    public class CardMatchingGameView : MonoBehaviour
    {
        [SerializeField]
        private CardMatchingBoardView _boardView;

        private ICardMatchingService _cardMatchingService;
        private CardView _firstSelected;
        private CardView _secondSelected;
        private bool _isBusy;

        public void StartGame(CardMatchingConfig config)
        {
            StartGameAsync(config).Forget();
        }

        private async UniTask StartGameAsync(CardMatchingConfig config)
        {
            await UniTask.WaitUntil(() => Engine.Initialized);

            _cardMatchingService = Engine.GetService<ICardMatchingService>();

            _cardMatchingService.OnCardsMatched += OnCardsMatched;
            _cardMatchingService.OnGameCompleted += OnGameCompleted;

            _cardMatchingService.StartGame(config.DefaultPairCount);
            _boardView.Init(_cardMatchingService.GetCards(), OnCardClicked);

            _firstSelected = null;
            _secondSelected = null;
            _isBusy = false;
        }

        private void OnCardClicked(CardView cardView)
        {
            if (_isBusy)
            {
                return;
            }

            var currentState = _cardMatchingService.GetCurrentState();

            if (currentState == CardMatchingState.WaitingForFirstCard)
            {
                _firstSelected = cardView;
                _boardView.SetSelected(_firstSelected, null);
            }
            else if (currentState == CardMatchingState.WaitingForSecondCard)
            {
                _secondSelected = cardView;
                _boardView.SetSelected(_firstSelected, _secondSelected);
                _isBusy = true;
            }

            _cardMatchingService.SelectCard(cardView.GetCardData());
        }

        private void OnCardsMatched(CardData first, CardData second, bool isMatch)
        {
            if (isMatch)
            {
                _boardView.UpdateAllCards();
                _boardView.ResetSelection();
                _isBusy = false;
            }
            else
            {
                StartCoroutine(HandleMismatchCoroutine());
            }
        }

        private IEnumerator HandleMismatchCoroutine()
        {
            _isBusy = true;

            yield return new WaitForSeconds(1.5f);

            _cardMatchingService.CompleteAnimation();

            _boardView.ResetSelection();

            _firstSelected = null;
            _secondSelected = null;

            _boardView.UpdateAllCards();

            _isBusy = false;
        }

        private void OnGameCompleted()
        {
            if (_cardMatchingService != null)
            {
                _cardMatchingService.OnCardsMatched -= OnCardsMatched;
                _cardMatchingService.OnGameCompleted -= OnGameCompleted;
            }

            // TODO: Temporary solution - just hide the object on game end. 
            // Replace with proper flow (results UI / Naninovel integration) later.
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_cardMatchingService != null)
            {
                _cardMatchingService.OnCardsMatched -= OnCardsMatched;
                _cardMatchingService.OnGameCompleted -= OnGameCompleted;
            }
        }
    }
}
