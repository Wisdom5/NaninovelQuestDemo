using System;
using System.Collections.Generic;
using Features.CardMatching.Scripts.Data;
using UnityEngine;

namespace Features.CardMatching.Scripts.Presentation
{
    public class CardMatchingBoardView : MonoBehaviour
    {
        [SerializeField]
        private Transform _cardParent;
        [SerializeField]
        private GameObject _cardPrefab;

        private readonly List<CardView> _cardViews = new();

        public void Init(List<CardData> cards, Action<CardView> onCardClick)
        {
            foreach (Transform child in _cardParent)
            {
                // TODO: pooling
                Destroy(child.gameObject);
            }

            _cardViews.Clear();

            foreach (var card in cards)
            {
                var cardGO = Instantiate(_cardPrefab, _cardParent);
                var cardView = cardGO.GetComponent<CardView>();

                cardView.Init(card, onCardClick);
                _cardViews.Add(cardView);
            }
        }

        public void UpdateAllCards()
        {
            foreach (var cardView in _cardViews)
            {
                cardView.UpdateView();
            }
        }

        public void SetSelected(CardView first, CardView second)
        {
            foreach (var cardView in _cardViews)
            {
                cardView.SetSelected(cardView == first || cardView == second);
            }
        }

        public void ResetSelection()
        {
            foreach (var cardView in _cardViews)
            {
                cardView.SetSelected(false);
            }
        }
    }
}
