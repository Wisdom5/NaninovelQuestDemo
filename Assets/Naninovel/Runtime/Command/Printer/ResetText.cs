// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets (clears) the contents of a text printer and optionally resets author ID.
    /// </summary>
    public class ResetText : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        /// <summary>
        /// Whether to also reset author of the currently printed text message.
        /// </summary>
        [ParameterDefaultValue("false")]
        public BooleanParameter ResetAuthor = false;

        protected override string AssignedPrinterId => PrinterId;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var printer = await GetOrAddPrinterAsync(asyncToken);
            printer.RevealProgress = 0f;
            printer.Text = LocalizableText.Empty;
            if (ResetAuthor) printer.AuthorId = null;
        }
    }
}
