// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Allows controlling scrollbar with Naninovel input.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollController : MonoBehaviour
    {
        private ScrollRect rect;
        private IInputSampler input;

        private void Awake ()
        {
            rect = GetComponent<ScrollRect>();
            input = Engine.GetService<IInputManager>().GetScrollY();
        }

        private void Update ()
        {
            if (input == null || Mathf.Approximately(0, input.Value)) return;
            rect.content.anchoredPosition += new Vector2(0, input.Value * rect.scrollSensitivity);
        }
    }
}
