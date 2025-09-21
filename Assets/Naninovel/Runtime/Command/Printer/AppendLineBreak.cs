// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a line break to a text printer.
    /// </summary>
    /// <remarks>
    /// Consider using `&lt;br&gt;` tag instead with [TMPro printers](/guide/text-printers#textmesh-pro).
    /// </remarks>
    [CommandAlias("br")]
    public class AppendLineBreak : PrinterCommand
    {
        /// <summary>
        /// Number of line breaks to add.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ParameterDefaultValue("1")]
        public IntegerParameter Count = 1;
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        /// <summary>
        /// ID of the actor, which should be associated with the appended line break.
        /// </summary>
        [ParameterAlias("author"), ActorContext(CharactersConfiguration.DefaultPathPrefix)]
        public StringParameter AuthorId;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => AuthorId;
        protected IUIManager UIManager => Engine.GetService<IUIManager>();

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var parts = new LocalizableTextPart[Count];
            for (int i = 0; i < Count; i++)
                parts[i] = LocalizableTextPart.FromPlainText("\n");
            var printer = await GetOrAddPrinterAsync(asyncToken);
            var text = new LocalizableText(parts);
            printer.Text += text;
            UIManager.GetUI<UI.IBacklogUI>()?.AppendMessage(text);
        }
    }
}
