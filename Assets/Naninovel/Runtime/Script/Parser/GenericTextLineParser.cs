// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using Naninovel.Parsing;

namespace Naninovel
{
    public class GenericTextLineParser : ScriptLineParser<GenericTextScriptLine, GenericLine>
    {
        protected virtual CommandParser CommandParser { get; }
        protected virtual GenericLine Model { get; private set; }
        protected virtual IList<Command> InlinedCommands { get; } = new List<Command>();
        protected virtual string AuthorId => Model.Prefix?.Author ?? "";
        protected virtual string AuthorAppearance => Model.Prefix?.Appearance ?? "";
        protected virtual PlaybackSpot Spot => new PlaybackSpot(ScriptName, LineIndex, InlinedCommands.Count);

        private readonly MixedValueParser mixedParser;
        private readonly IErrorHandler errorHandler;

        public GenericTextLineParser (ITextIdentifier identifier, IErrorHandler errorHandler = null)
        {
            this.errorHandler = errorHandler;
            mixedParser = new MixedValueParser(identifier);
            CommandParser = new CommandParser(identifier, errorHandler);
        }

        protected override GenericTextScriptLine Parse (GenericLine lineModel)
        {
            ResetState(lineModel);
            AddAppearanceChange();
            AddContent();
            AddLastWaitInput();
            return new GenericTextScriptLine(InlinedCommands, LineIndex, LineHash);
        }

        protected virtual void ResetState (GenericLine model)
        {
            Model = model;
            InlinedCommands.Clear();
        }

        protected virtual void AddAppearanceChange ()
        {
            if (string.IsNullOrEmpty(AuthorId)) return;
            if (string.IsNullOrEmpty(AuthorAppearance)) return;
            AddCommand(new ModifyCharacter {
                Id = AuthorId,
                Appearance = AuthorAppearance,
                Wait = false,
                PlaybackSpot = Spot
            });
        }

        protected virtual void AddContent ()
        {
            foreach (var content in Model.Content)
                if (content is InlinedCommand inlined) AddCommand(inlined.Command);
                else AddGenericText(content as MixedValue);
        }

        protected virtual void AddCommand (Parsing.Command commandModel)
        {
            var spot = new PlaybackSpot(ScriptName, LineIndex, InlinedCommands.Count);
            var args = new CommandParseArgs(commandModel, spot, Transient);
            var command = CommandParser.Parse(args);
            AddCommand(command);
        }

        protected virtual void AddCommand (Command command)
        {
            // Route [i] after printed text to wait input param of the print command.
            if (command is WaitForInput && InlinedCommands.LastOrDefault() is PrintText print)
                print.WaitForInput = true;
            else InlinedCommands.Add(command);
        }

        protected virtual void AddGenericText (MixedValue genericText)
        {
            var printedBefore = InlinedCommands.Any(c => c is PrintText);
            var print = (PrintText)Activator.CreateInstance(Command.ResolveCommandType("print"));
            var raw = mixedParser.Parse(genericText, !Transient);
            print.Text = CommandParameter.FromRaw<LocalizableTextParameter>(raw, Spot, out var errors);
            if (errors != null) errorHandler?.HandleError(new ParseError(errors, 0, 0));
            if (!string.IsNullOrEmpty(AuthorId)) print.AuthorId = AuthorId;
            if (printedBefore) print.ResetPrinter = false;
            if (printedBefore) print.LineBreaks = 0;
            print.Wait = true;
            print.WaitForInput = false;
            print.PlaybackSpot = Spot;
            AddCommand(print);
        }

        protected virtual void AddLastWaitInput ()
        {
            if (!InlinedCommands.Any(c => c is PrintText)) return;
            if (InlinedCommands.Any(c => c is SkipInput)) return;
            if (InlinedCommands.LastOrDefault() is WaitForInput) return;
            if (InlinedCommands.LastOrDefault() is PrintText print)
                print.WaitForInput = true;
            else AddCommand(new WaitForInput { PlaybackSpot = Spot });
        }
    }
}
