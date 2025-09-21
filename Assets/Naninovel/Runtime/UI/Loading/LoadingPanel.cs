// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class LoadingPanel : CustomUI, ILoadingUI
    {
        [Tooltip("Event invoked when script preload progress is changed, in 0.0 to 1.0 range.")]
        [SerializeField] private FloatUnityEvent onProgressChanged;

        private IScriptPlayer scriptPlayer;

        protected override void Awake ()
        {
            base.Awake();

            scriptPlayer = Engine.GetService<IScriptPlayer>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            scriptPlayer.OnPreloadProgress += HandleProgressChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (scriptPlayer != null)
                scriptPlayer.OnPreloadProgress -= HandleProgressChanged;
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);
            onProgressChanged?.Invoke(0);
        }

        protected virtual void HandleProgressChanged (float value) => onProgressChanged?.Invoke(value);
    }
}
