// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents .nani scenario script file serialized as Unity asset.
    /// </summary>
    [Serializable]
    public class Script : ScriptableObject
    {
        /// <summary>
        /// Name of the script asset (equals local resource path).
        /// </summary>
        public string Name => name;
        /// <summary>
        /// The list of lines this script asset contains, in order.
        /// </summary>
        public IReadOnlyList<ScriptLine> Lines => lines;
        /// <summary>
        /// Map of identified (localizable) text contained in the script.
        /// </summary>
        public ScriptTextMap TextMap => textMap;

        private static IScriptParser cachedParser;

        [SerializeReference] private ScriptLine[] lines;
        [SerializeField] private ScriptTextMap textMap;

        /// <summary>
        /// Creates new script asset instance from parsed (compiled) lines.
        /// </summary>
        /// <param name="name">Name of the script asset; should equal local resource path.</param>
        /// <param name="lines">Parsed lines of the script, in order.</param>
        /// <param name="textMap">Map of identified (localizable) text contained in the script.</param>
        public static Script Create (string name, ScriptLine[] lines, ScriptTextMap textMap)
        {
            var asset = CreateInstance<Script>();
            asset.name = name;
            asset.lines = lines;
            asset.textMap = textMap;
            return asset;
        }

        /// <summary>
        /// Creates new script asset instance based on specified script text.
        /// </summary>
        /// <param name="scriptName">Name of the script asset; should equal local resource path.</param>
        /// <param name="scriptText">The script text to parse.</param>
        /// <param name="filePath">File path of the script; used when logging parse errors.</param>
        public static Script FromText (string scriptName, string scriptText, string filePath = null)
        {
            var logger = ScriptParseErrorLogger.GetFor(filePath ?? scriptName);
            var script = GetCachedParser().ParseText(scriptName, scriptText, new ParseOptions(logger, false));
            ScriptParseErrorLogger.Return(logger);
            return script;
        }

        /// <summary>
        /// Creates new transient script instance based on specified script text.
        /// </summary>
        /// <remarks>
        /// Use this method when creating scripts at runtime to prevent localizable text resolve errors.
        /// </remarks>
        /// <param name="scriptName">Name of the script; will be mentioned in parse error log.</param>
        /// <param name="scriptText">The script text to parse.</param>
        public static Script FromTransient (string scriptName, string scriptText)
        {
            var logger = ScriptParseErrorLogger.GetFor($"Transient/{scriptName}");
            var script = GetCachedParser().ParseText(scriptName, scriptText, new ParseOptions(logger, true));
            ScriptParseErrorLogger.Return(logger);
            return script;
        }

        /// <summary>
        /// Collects all the contained commands (preserving the order).
        /// </summary>
        public List<Command> ExtractCommands ()
        {
            var commands = new List<Command>();
            foreach (var line in lines)
                if (line is CommandScriptLine commandLine)
                    commands.Add(commandLine.Command);
                else if (line is GenericTextScriptLine genericLine)
                    commands.AddRange(genericLine.InlinedCommands);
            return commands;
        }

        /// <summary>
        /// Returns first script line of <typeparamref name="TLine"/> filtered by <paramref name="predicate"/> or null.
        /// </summary>
        public TLine FindLine<TLine> (Predicate<TLine> predicate) where TLine : ScriptLine
        {
            return lines.FirstOrDefault(l => l is TLine tline && predicate(tline)) as TLine;
        }

        /// <summary>
        /// Returns all the script lines of <typeparamref name="TLine"/> filtered by <paramref name="predicate"/>.
        /// </summary>
        public List<TLine> FindLines<TLine> (Predicate<TLine> predicate) where TLine : ScriptLine
        {
            return lines.Where(l => l is TLine tline && predicate(tline)).Cast<TLine>().ToList();
        }

        /// <summary>
        /// Checks whether a <see cref="LabelScriptLine"/> with the provided value exists in this script.
        /// </summary>
        public bool LabelExists (string label)
        {
            foreach (var line in lines)
                if (line is LabelScriptLine labelLine && labelLine.LabelText.EqualsFast(label))
                    return true;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve index of a <see cref="LabelScriptLine"/> with the provided <see cref="LabelScriptLine.LabelText"/>.
        /// Returns -1 in case the label is not found.
        /// </summary>
        public int GetLineIndexForLabel (string label)
        {
            foreach (var line in lines)
                if (line is LabelScriptLine labelLine && labelLine.LabelText.EqualsFast(label))
                    return labelLine.LineIndex;
            return -1;
        }

        /// <summary>
        /// Returns first <see cref="LabelScriptLine.LabelText"/> located above line with the provided index.
        /// Returns null when not found.
        /// </summary>
        public string GetLabelForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is LabelScriptLine labelLine)
                    return labelLine.LabelText;
            return null;
        }

        /// <summary>
        /// Returns first <see cref="CommentScriptLine.CommentText"/> located above line with the provided index.
        /// Returns null when not found.
        /// </summary>
        public string GetCommentForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is CommentScriptLine commentLine)
                    return commentLine.CommentText;
            return null;
        }

        private static IScriptParser GetCachedParser ()
        {
            var typeName = Configuration.GetOrDefault<ScriptsConfiguration>().ScriptParser;
            if (cachedParser != null && cachedParser.GetType().AssemblyQualifiedName == typeName) return cachedParser;
            var type = Type.GetType(typeName);
            if (type is null) throw new Error($"Failed to create type from '{typeName}'.");
            cachedParser = Activator.CreateInstance(type) as IScriptParser;
            return cachedParser;
        }
    }
}
