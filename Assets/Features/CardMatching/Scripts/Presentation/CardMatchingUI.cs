using Features.CardMatching.Scripts.Data;
using Features.CardMatching.Scripts.Presentation;
using Naninovel.UI;
using UnityEngine;

namespace Features.CardMatching.Scripts.UI
{
    public class CardMatchingUI : CustomUI
    {
        private const string CARD_MATCHING_GAME_CANVAS = "CardMatchingGameCanvas";

        [SerializeField]
        private CardMatchingGameView _gameView;
        [SerializeField]
        private CardMatchingConfig _defaultConfig;

        protected override void Start()
        {
            var obj = GameObject.Find(CARD_MATCHING_GAME_CANVAS);
            _gameView = obj?.GetComponentInChildren<CardMatchingGameView>();

            if (_gameView == null)
            {
                Debug.LogError("[CardMatchingUI] Could not find CardMatchingGameView.");
            }
        }

        public void StartCardGame()
        {
            if (_gameView != null && _defaultConfig != null)
            {
                _gameView.StartGame(_defaultConfig);

                Debug.Log("[CardMatchingUI] Starting game");
            }
            else
            {
                Debug.LogError("[CardMatchingUI] Error starting game. _gameView and  default config are missing.");
            }
        }
    }
}
