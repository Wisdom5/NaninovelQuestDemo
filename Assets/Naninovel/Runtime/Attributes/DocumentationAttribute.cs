// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Used by Naninovel editor tools to extract documentation from custom types (eg, commands).
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class DocumentationAttribute : Attribute
    {
        public readonly string Summary;
        public readonly string Remarks;

        public DocumentationAttribute (string summary, string remarks = null)
        {
            Summary = summary;
            Remarks = remarks;
        }
    }
}
