// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.ManagedText;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <summary>
    /// Generates localization documents for managed text.
    /// </summary>
    public class ManagedTextLocalizer
    {
        public class Options
        {
            public Action<string> OnUntranslated { get; set; }
            public LocalizableTextAnnotations Annotations { get; set; }
            public bool Multiline { get; set; }
            public int Spacing { get; set; } = 1;
        }

        private readonly List<ManagedTextRecord> records = new List<ManagedTextRecord>();
        private readonly Options options;
        private readonly Func<string, ManagedTextDocument> parse;
        private readonly Func<ManagedTextDocument, string> serialize;

        public ManagedTextLocalizer (Options options = null)
        {
            this.options = options ?? new Options();
            parse = this.options.Multiline
                ? new MultilineManagedTextParser().Parse
                : (Func<string, ManagedTextDocument>)new InlineManagedTextParser().Parse;
            serialize = this.options.Multiline
                ? new MultilineManagedTextSerializer(this.options.Spacing).Serialize
                : (Func<ManagedTextDocument, string>)new InlineManagedTextSerializer(this.options.Spacing).Serialize;
        }

        /// <summary>
        /// Generates localization document for specified serialized source managed text document string.
        /// When existing document is provided will preserve existing records.
        /// </summary>
        public string Localize (string source, string existing = null)
        {
            return Localize(parse(source).Records, existing != null ? parse(existing).Records : null);
        }

        /// <summary>
        /// Generates localization document for specified source managed text records.
        /// When existing records are provided will preserve them.
        /// </summary>
        public string Localize (IReadOnlyCollection<ManagedTextRecord> source, IReadOnlyCollection<ManagedTextRecord> existing = null)
        {
            records.Clear();
            foreach (var record in source)
            {
                var value = ExistingOrEmpty(record.Key, existing);
                if (string.IsNullOrEmpty(value)) options.OnUntranslated?.Invoke(record.Key);
                records.Add(new ManagedTextRecord(record.Key, value, BuildAnnotation(record)));
            }
            return serialize(new ManagedTextDocument(records));
        }

        private string ExistingOrEmpty (string key, IReadOnlyCollection<ManagedTextRecord> existing)
        {
            if (existing is null) return "";
            var record = existing.FirstOrDefault(r => r.Key == key);
            if (string.IsNullOrEmpty(record.Key)) return "";
            return record.Value;
        }

        private string BuildAnnotation (ManagedTextRecord record)
        {
            if (options.Annotations is null || !options.Annotations.TryGet(record.Key, out var annotation))
                if (string.IsNullOrEmpty(record.Comment)) return record.Value;
                else return $"{record.Value}{Identifiers.CommentLine}{Identifiers.CommentLine} {record.Comment}";
            return $"{record.Value}{Identifiers.CommentLine}{Identifiers.CommentLine} {annotation}";
        }
    }
}
