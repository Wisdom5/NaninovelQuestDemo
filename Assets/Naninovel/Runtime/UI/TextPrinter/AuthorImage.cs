// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents image of the current text message author (character) avatar.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AuthorImage : ScriptableUIBehaviour
    {
        private ImageCrossfader crossfader;

        /// <summary>
        /// Cross-fades current image's texture with the provided one over <see cref="ScriptableUIBehaviour.FadeTime"/>.
        /// When null is provided, will hide the image instead.
        /// </summary>
        public virtual UniTask ChangeTextureAsync (Texture texture)
        {
            return crossfader?.CrossfadeAsync(texture, FadeTime) ?? UniTask.CompletedTask;
        }

        protected override void Awake ()
        {
            base.Awake();

            if (TryGetComponent<RawImage>(out var image))
                crossfader = new ImageCrossfader(image);
        }

        protected override void OnDestroy ()
        {
            crossfader?.Dispose();
            base.OnDestroy();
        }
    }
}
