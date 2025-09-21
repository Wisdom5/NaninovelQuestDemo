// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.UI
{
    /// <inheritdoc cref="IPauseUI"/>
    public class PauseUI : CustomUI, IPauseUI
    {
        private IInputManager input => Engine.GetService<IInputManager>();

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (input.TryGetSampler(InputNames.Pause, out var pause))
                pause.OnStart += ToggleVisibility;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (input.TryGetSampler(InputNames.Pause, out var pause))
                pause.OnStart -= ToggleVisibility;
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (input.TryGetSampler(InputNames.Cancel, out var cancel))
                if (visible) cancel.OnEnd += Hide;
                else cancel.OnEnd -= Hide;
        }
    }
}
