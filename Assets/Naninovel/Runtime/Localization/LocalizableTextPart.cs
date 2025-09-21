// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Parsing;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a part of <see cref="LocalizableText"/>.
    /// Can be either ID of a localizable text or plain text chunk of the resulting string.
    /// </summary>
    /// <remarks>
    /// Mixed semantic hack is required to support Unity's serialization,
    /// where interfaces can't be serialized by value.
    /// </remarks>
    [Serializable]
    public struct LocalizableTextPart : IEquatable<LocalizableTextPart>
    {
        /// <summary>
        /// Unique (inside script) persistent identifier of the associated localizable text.
        /// Can be null, in which case <see cref="Text"/> should be used.
        public string Id => PlainText ? throw new Error("Text should be used instead.") : id;
        /// <summary>
        /// Name (local resource path) of the scenario script containing associated text.
        /// Can be null, in which case <see cref="Text"/> should be used.
        public string Script => PlainText ? throw new Error("Text should be used instead.") : script;
        /// <summary>
        /// Plain (non-localizable) text chunk of the resulting string.
        /// Can be null, in which case <see cref="Id"/> should be used.
        /// </summary>
        public string Text => PlainText ? text : throw new Error("Id should be used instead.");
        /// <summary>
        /// Whether the part represents plain (non-localizable) text and <see cref="Text"/> should be used.
        /// </summary>
        public bool PlainText => string.IsNullOrEmpty(id);

        [SerializeField] private string id;
        [SerializeField] private string script;
        [SerializeField] private string text;

        public static LocalizableTextPart FromId (string id, string scriptName)
        {
            return new LocalizableTextPart { id = id, script = scriptName, text = null };
        }

        public static LocalizableTextPart FromPlainText (string text)
        {
            return new LocalizableTextPart { id = null, script = null, text = text };
        }

        public override string ToString ()
        {
            if (PlainText) return Text;
            return $"{Identifiers.TextIdOpen}{Script}/{Id}{Identifiers.TextIdClose}";
        }

        public bool Equals (LocalizableTextPart other)
        {
            return id == other.id && script == other.script && text == other.text;
        }

        public override bool Equals (object obj)
        {
            return obj is LocalizableTextPart other && Equals(other);
        }

        public override int GetHashCode ()
        {
            unchecked
            {
                var hashCode = id != null ? id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (script != null ? script.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (text != null ? text.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
