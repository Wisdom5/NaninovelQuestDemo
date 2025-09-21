// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Allows resolving localized strings from <see cref="LocalizableText"/>.
    /// </summary>
    public interface ITextLocalizer : IEngineService
    {
        /// <summary>
        /// Returns localized string associated with the specified reference.
        /// </summary>
        /// <remarks>
        /// When running under non-source locale and requested text is not localized will return source text;
        /// when under source locale and mapped text is missing will return boilerplate string with IDs.
        /// </remarks>
        string Resolve (LocalizableText text);
    }
}
