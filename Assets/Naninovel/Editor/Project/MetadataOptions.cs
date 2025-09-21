// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Naninovel
{
    /// <summary>
    /// Options of a metadata generation operation.
    /// </summary>
    public class MetadataOptions
    {
        /// <summary>
        /// Types of the commands the metadata is being generated for.
        /// </summary>
        public IReadOnlyCollection<Type> Commands { get; }
        /// <summary>
        /// Can be used to extract documentation for a command type.
        /// </summary>
        public Func<Type, CommandDocumentation> GetCommandDocumentation { get; }
        /// <summary>
        /// Can be used to extract documentation for parameter field.
        /// </summary>
        public Func<FieldInfo, string> GetParameterDocumentation { get; }
        /// <summary>
        /// Notifies of the current activity and progress.
        /// </summary>
        public Action<string, float> NotifyProgress { get; }

        internal MetadataOptions (IReadOnlyCollection<Type> commands, Action<string, float> notifyProgress,
            Func<Type, CommandDocumentation> getCommandDoc, Func<FieldInfo, string> getParamDoc)
        {
            Commands = commands;
            NotifyProgress = notifyProgress;
            GetCommandDocumentation = getCommandDoc;
            GetParameterDocumentation = getParamDoc;
        }
    }
}
