// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.ManagedText;

namespace Naninovel
{
    /// <summary>
    /// Provides managed text records and manages fields with <see cref="ManagedTextAttribute"/>.
    /// </summary>
    public interface ITextManager : IEngineService<ManagedTextConfiguration>
    {
        /// <summary>
        /// Returns all currently available managed text categories.
        /// </summary>
        IReadOnlyCollection<string> GetAllCategories ();
        /// <summary>
        /// Returns all currently available managed text records of the specified category.
        /// </summary>
        IReadOnlyCollection<ManagedTextRecord> GetAllRecords (string category);
        /// <summary>
        /// Attempts to retrieve managed text record with specified key and category (document resource local path);
        /// returns false when no records found.
        /// </summary>
        bool TryGetRecord (string key, string category, out ManagedTextRecord record);
    }
}
