// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ITextManager"/>.
    /// </summary>
    public static class TextManagerExtensions
    {
        /// <summary>
        /// Attempts to retrieve value of managed text record with specified key and category (document resource local path);
        /// returns null when no records found or associated document is not loaded.
        /// </summary>
        public static string GetRecordValue (this ITextManager manager, string key, string category)
        {
            return manager.TryGetRecord(key, category, out var record) ? record.Value : null;
        }

        /// <summary>
        /// Attempts to retrieve value of managed text record with specified key and category (document resource local path);
        /// returns record's comment or category/key when record is not available.
        /// </summary>
        public static string GetRecordValueWithFallback (this ITextManager manager, string key, string category)
        {
            var value = GetRecordValue(manager, key, category);
            if (value is null) return $"{category}/{key}";
            if (value.Length == 0 && manager.TryGetRecord(key, category, out var record))
                if (!string.IsNullOrEmpty(record.Comment)) return record.Comment;
                else return $"{category}/{key}";
            return value;
        }
    }
}
