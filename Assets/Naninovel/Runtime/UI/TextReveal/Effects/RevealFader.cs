// Copyright 2023 ReWaffle LLC. All rights reserved.

using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// A text reveal effect that fades-in revealing characters.
    /// </summary>
    public class RevealFader : TextRevealEffect
    {
        [Tooltip("How long to stretch fade gradient, by character."), Range(0, 100)]
        [SerializeField] private float length = 10;
        [Tooltip("When below 1, will modify opacity of the text before the last character from which reveal started."), Range(0, 1)]
        [SerializeField] private float slackOpacity = 1;
        [Tooltip("Duration (in seconds) of fading slack text to the target opacity."), Range(0, 3)]
        [SerializeField] private float slackDuration = 0.5f;

        private void OnEnable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
            Info.OnChange += Update;
        }

        private void OnDisable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            if (Text) Info.OnChange -= Update;
        }

        private void Update ()
        {
            for (int i = 0; i < Text.textInfo.characterCount; i++)
                FadeCharacter(Text.textInfo.characterInfo[i], EvaluateOpacity(i));
            Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void FadeCharacter (TMP_CharacterInfo info, byte opacity)
        {
            if (!info.isVisible) return;
            var colors = Text.textInfo.meshInfo[info.materialReferenceIndex].colors32;
            for (int i = 0; i < 4; i++)
                colors[info.vertexIndex + i].a = opacity;
        }

        private byte EvaluateOpacity (int charIndex)
        {
            if (!IsSlack(charIndex)) return (byte)(Info.GetRevealRatio(charIndex, length) * byte.MaxValue);
            return (byte)(byte.MaxValue - Mathf.Clamp01((Engine.Time.Time - Info.GetAppendTime(charIndex)) / slackDuration) * (1f - slackOpacity) * byte.MaxValue);
        }

        private bool IsSlack (int charIndex)
        {
            return slackOpacity < 1 && charIndex <= Info.LastAppendIndex;
        }

        private void HandleTextChanged (Object obj)
        {
            if (obj == Text) Update();
        }
    }
}
