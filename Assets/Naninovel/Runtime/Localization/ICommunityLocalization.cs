// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Allows accessing localization resources authored externally by players for the built game.
    /// </summary>
    public interface ICommunityLocalization : IEngineService
    {
        /// <summary>
        /// Whether the community localization is currently in effect.
        /// </summary>
        bool Active { get; }
        /// <summary>
        /// Author of the active localization.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Loads localized script and managed text documents.
        /// </summary>
        UniTask<IReadOnlyList<(string Text, string Category)>> LoadLocalizedDocumentsAsync ();
    }
}
