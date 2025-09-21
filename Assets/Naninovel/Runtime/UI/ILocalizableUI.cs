// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.UI
{
    /// <summary>
    /// Implementing <see cref="IManagedUI"/> is notified when localization is changed.
    /// </summary>
    public interface ILocalizableUI
    {
        UniTask HandleLocalizationChangedAsync ();
    }
}
