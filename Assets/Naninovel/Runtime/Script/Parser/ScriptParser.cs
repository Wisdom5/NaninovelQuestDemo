// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptParser"/>
    public class ScriptParser : IScriptParser
    {
        protected virtual CommentLineParser CommentLineParser { get; }
        protected virtual LabelLineParser LabelLineParser { get; }
        protected virtual CommandLineParser CommandLineParser { get; }
        protected virtual GenericTextLineParser GenericTextLineParser { get; }

        private readonly Lexer lexer = new Lexer();
        private readonly List<ScriptLine> lines = new List<ScriptLine>();
        private readonly ParseErrorHandler errorHandler = new ParseErrorHandler();
        private readonly TextMapper textMapper = new TextMapper();
        private readonly Parsing.ScriptParser modelParser;

        public ScriptParser ()
        {
            CommentLineParser = new CommentLineParser();
            LabelLineParser = new LabelLineParser();
            CommandLineParser = new CommandLineParser(textMapper, errorHandler);
            GenericTextLineParser = new GenericTextLineParser(textMapper, errorHandler);
            modelParser = new Parsing.ScriptParser(new ParseHandlers { ErrorHandler = errorHandler, TextIdentifier = textMapper });
        }

        public virtual Script ParseText (string scriptName, string scriptText, ParseOptions options = default)
        {
            Reset(options);
            var textLines = Parsing.ScriptParser.SplitText(scriptText);
            for (int i = 0; i < textLines.Length; i++)
                lines.Add(ParseLine(i, textLines[i]));
            return Script.Create(scriptName, lines.ToArray(), CreateTextMap());

            ScriptLine ParseLine (int lineIndex, string lineText)
            {
                if (string.IsNullOrWhiteSpace(lineText))
                    return new EmptyScriptLine(lineIndex);
                errorHandler.LineIndex = lineIndex;
                switch (modelParser.ParseLine(lineText))
                {
                    case CommentLine comment: return ParseCommentLine(new LineParseArgs<CommentLine>(scriptName, lineText, lineIndex, options.Transient, comment));
                    case LabelLine label: return ParseLabelLine(new LineParseArgs<LabelLine>(scriptName, lineText, lineIndex, options.Transient, label));
                    case CommandLine command: return ParseCommandLine(new LineParseArgs<CommandLine>(scriptName, lineText, lineIndex, options.Transient, command));
                    case GenericLine generic: return ParseGenericTextLine(new LineParseArgs<GenericLine>(scriptName, lineText, lineIndex, options.Transient, generic));
                    default: throw new Error($"Unknown line type: {lineText}");
                }
            }
        }

        protected virtual CommentScriptLine ParseCommentLine (LineParseArgs<CommentLine> args)
        {
            return CommentLineParser.Parse(args);
        }

        protected virtual LabelScriptLine ParseLabelLine (LineParseArgs<LabelLine> args)
        {
            return LabelLineParser.Parse(args);
        }

        protected virtual CommandScriptLine ParseCommandLine (LineParseArgs<CommandLine> args)
        {
            return CommandLineParser.Parse(args);
        }

        protected virtual GenericTextScriptLine ParseGenericTextLine (LineParseArgs<GenericLine> args)
        {
            return GenericTextLineParser.Parse(args);
        }

        protected virtual ScriptTextMap CreateTextMap ()
        {
            return new ScriptTextMap(textMapper.Map.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        private void Reset (ParseOptions options)
        {
            errorHandler.Errors = options.Errors;
            lines.Clear();
            textMapper.Clear();
        }
    }
}
