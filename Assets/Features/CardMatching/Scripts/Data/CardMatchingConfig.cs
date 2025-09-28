using Naninovel;
using UnityEngine;

namespace Features.CardMatching.Scripts.Data
{
    [CreateAssetMenu(fileName = "CardMatchingConfig", menuName = "Game/Card Matching Config")]
    public class CardMatchingConfig : Configuration
    {
        public int DefaultPairCount = 6;
    }
}
