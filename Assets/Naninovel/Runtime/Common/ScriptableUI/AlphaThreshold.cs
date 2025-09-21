// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class AlphaThreshold : MonoBehaviour
    {
        [SerializeField] private Image graphic;
        [SerializeField] private float minimumThreshold;

        private void Start ()
        {
            if (graphic) graphic.alphaHitTestMinimumThreshold = minimumThreshold;
        }
    }
}
