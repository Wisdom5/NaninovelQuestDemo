// Copyright 2023 ReWaffle LLC. All rights reserved.

using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Wrapper over <see cref="NaninovelTMProText"/> with support for gradual text reveal.
    /// </summary>
    public class RevealableText : NaninovelTMProText, IRevealableText
    {
        public virtual IRevealInfo RevealInfo => RevealState;
        public virtual string Text { get => text; set => SetText(value); }
        public virtual Color TextColor { get => color; set => color = value; }
        public virtual float RevealProgress { get => GetRevealProgress(); set => SetRevealProgress(value); }

        protected virtual RevealState RevealState { get; } = new RevealState();
        protected virtual string TMPText { get => base.text; set => base.text = value; }
        protected virtual int LastCharIndex => Mathf.Max(-1, textInfo.characterCount - 1);
        protected virtual int LastRevealedCharIndex => RevealState.LastRevealedCharIndex;
        protected virtual bool RevealRubyInstantly => revealRubyInstantly;

        [Tooltip("Whether to reveal characters under ruby (furigana) tag instantly.")]
        [SerializeField] private bool revealRubyInstantly = true;

        private new string text = "";

        public virtual void RevealNextChars (int count, float duration, AsyncToken token)
        {
            RevealState.SetLast(GetNextRevealable(LastRevealedCharIndex + count), duration, token);
        }

        public virtual Vector2 GetLastRevealedCharPosition ()
        {
            if (!TryGetCharInfo(GetPreviousNonRuby(LastRevealedCharIndex), out var info)) return default;
            return rectTransform.TransformPoint(isRightToLeftText ? info.bottomLeft : info.bottomRight);
        }

        public virtual char GetLastRevealedChar ()
        {
            return TryGetCharInfo(LastRevealedCharIndex, out var info) ? info.character : default;
        }

        protected override void Awake ()
        {
            base.Awake();
            text = TMPText;
        }

        protected virtual void SetText (string value)
        {
            if (value == text) return;
            RevealState.SetLatAppendIndex(LastCharIndex);
            text = TMPText = value;
            // Forced update is required to report correct reveal progress right after text is changed; 
            // otherwise the update (which feeds TMP_Text.textInfo) is delayed by the end of frame.
            ForceMeshUpdate();
        }

        protected virtual float GetRevealProgress ()
        {
            if (LastCharIndex <= 0) return LastRevealedCharIndex >= 0 ? 1 : 0;
            return Mathf.Clamp01(LastRevealedCharIndex / (float)LastCharIndex);
        }

        protected virtual void SetRevealProgress (float progress)
        {
            if (LastCharIndex < 0 || Mathf.Approximately(progress, 0)) RevealState.Reset();
            else RevealState.SetLast(GetNextRevealable(Mathf.CeilToInt(LastCharIndex * Mathf.Clamp01(progress))));
        }

        protected virtual int GetNextRevealable (int charIndex)
        {
            if (RevealRubyInstantly) charIndex = GetNextNonRuby(charIndex);
            return Mathf.Min(LastCharIndex, charIndex);
        }

        protected virtual bool TryGetCharInfo (int charIndex, out TMP_CharacterInfo info)
        {
            var valid = charIndex >= 0 && textInfo.characterCount > charIndex;
            info = valid ? textInfo.characterInfo[charIndex] : default;
            return valid;
        }

        protected virtual int GetPreviousNonRuby (int charIndex)
        {
            for (int i = charIndex; i >= 0; i--)
                if (!IsRuby(i))
                    return i;
            return charIndex;
        }

        protected virtual int GetNextNonRuby (int charIndex)
        {
            for (int i = charIndex; i <= LastCharIndex; i++)
                if (!IsRuby(i))
                    return i;
            return LastCharIndex;
        }
    }
}
