using System;
using Features.CardMatching.Scripts.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.CardMatching.Scripts.Presentation
{
    public class CardView : MonoBehaviour
    {
        [SerializeField]
        private Button _button;
        [SerializeField]
        private Image _frontImage;
        [SerializeField]
        private Image _backImage;

        private CardData _cardData;
        private Action<CardView> _onClick;

        public bool IsSelected { get; private set; }

        public void Init(CardData cardData, Action<CardView> onClick)
        {
            _cardData = cardData;
            _onClick = onClick;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
            IsSelected = false;

            SetCardSprite();
            UpdateView();

            Debug.Log($"[CardView] Card initialized: PairId = {_cardData.PairId}");
        }

        private void SetCardSprite()
        {
            var spritePath = $"UI/Sprites/Card_{_cardData.PairId}";
            var cardSprite = Resources.Load<Sprite>(spritePath);

            if (cardSprite != null)
            {
                _frontImage.sprite = cardSprite;
            }
            else
            {
                Color[] colors =
                {
                    Color.red, Color.blue, Color.green, Color.yellow,
                    Color.magenta, Color.cyan, new(1f, 0.5f, 0f), Color.gray,
                    new(1f, 0f, 0.5f), new(0.5f, 1f, 0f)
                };

                _frontImage.color = colors[_cardData.PairId % colors.Length];
            }

            var backSpritePath = "UI/Sprites/CardBack";
            var backSprite = Resources.Load<Sprite>(backSpritePath);

            if (backSprite != null)
            {
                _backImage.sprite = backSprite;
            }
            else
            {
                _backImage.color = Color.white;
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            UpdateView();
        }

        public void UpdateView()
        {
            var isFaceUp = _cardData.IsMatched || IsSelected;
            _frontImage.gameObject.SetActive(isFaceUp);
            _backImage.gameObject.SetActive(!isFaceUp);
            _button.interactable = !_cardData.IsMatched;
        }

        private void OnClick()
        {
            _onClick?.Invoke(this);
        }

        public CardData GetCardData()
        {
            return _cardData;
        }
    }
}
