// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Naninovel.ManagedText;

namespace Naninovel
{
    public static class ManagedTextUtils
    {
        public const BindingFlags ManagedFieldBindings = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly InlineManagedTextParser inlineParser = new InlineManagedTextParser();
        private static readonly MultilineManagedTextParser multilineParser = new MultilineManagedTextParser();

        /// <summary>
        /// Converts map of ID -> text items into records hashset.
        /// </summary>
        public static HashSet<ManagedTextRecord> HashMap (IReadOnlyDictionary<string, string> map)
        {
            return new HashSet<ManagedTextRecord>(map.Select(kv => new ManagedTextRecord(kv.Key, kv.Value)));
        }

        /// <summary>
        /// Parses specified managed text document text.
        /// When <paramref name="category"/> is specified, will pick appropriate parser (inline or multiline),
        /// otherwise will attempt to detect the format automatically.
        /// </summary>
        public static ManagedTextDocument Parse (string documentText, string category = null)
        {
            var multi = string.IsNullOrEmpty(category)
                ? ManagedTextDetector.IsMultiline(documentText)
                : ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().IsMultilineCategory(category);
            return multi ? multilineParser.Parse(documentText) : inlineParser.Parse(documentText);
        }

        /// <summary>
        /// Serializes specified managed text document into text string.
        /// When <paramref name="category"/> is specified, will pick appropriate serializer (inline or multiline).
        /// </summary>
        public static string Serialize (ManagedTextDocument document, string category = null, int spacing = 1)
        {
            var multi = ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().IsMultilineCategory(category);
            return multi
                ? new MultilineManagedTextSerializer(spacing).Serialize(document)
                : new InlineManagedTextSerializer(spacing).Serialize(document);
        }
    }
}
