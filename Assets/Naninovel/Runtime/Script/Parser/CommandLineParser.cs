// Copyright 2023 ReWaffle LLC. All rights reserved.

using Naninovel.Parsing;

namespace Naninovel
{
    public class CommandLineParser : ScriptLineParser<CommandScriptLine, CommandLine>
    {
        protected virtual CommandParser CommandParser { get; }

        public CommandLineParser (ITextIdentifier identifier, IErrorHandler errorHandler = null)
        {
            CommandParser = new CommandParser(identifier, errorHandler);
        }

        protected override CommandScriptLine Parse (CommandLine lineModel)
        {
            var spot = new PlaybackSpot(ScriptName, LineIndex, 0);
            var args = new CommandParseArgs(lineModel.Command, spot, Transient);
            var command = CommandParser.Parse(args);
            return new CommandScriptLine(command, LineIndex, LineHash);
        }
    }
}
