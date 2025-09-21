// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ILocalizationManager"/>
    /// and associated types.
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Whether <see cref="LocalizationConfiguration.SourceLocale"/> is currently selected.
        /// </summary>
        public static bool IsSourceLocaleSelected (this ILocalizationManager manager)
        {
            return manager.SelectedLocale == manager.Configuration.SourceLocale;
        }
    }
}
