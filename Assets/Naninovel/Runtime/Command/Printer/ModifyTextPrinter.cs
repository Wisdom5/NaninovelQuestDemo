// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [text printer actor](/guide/text-printers).
    /// </summary>
    [CommandAlias("printer")]
    [ActorContext(TextPrintersConfiguration.DefaultPathPrefix, paramId: "Id")]
    public class ModifyTextPrinter : ModifyOrthoActor<ITextPrinterActor, TextPrinterState, TextPrinterMetadata, TextPrintersConfiguration, ITextPrinterManager>
    {
        /// <summary>
        /// ID of the printer to modify and the appearance to set. 
        /// When ID or appearance are not provided, will use default ones.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), ActorContext(TextPrintersConfiguration.DefaultPathPrefix, 0), AppearanceContext(1)]
        public NamedStringParameter IdAndAppearance;
        /// <summary>
        /// Whether to make the printer the default one.
        /// Default printer will be subject of all the printer-related commands when `printer` parameter is not specified.
        /// </summary>
        [ParameterAlias("default"), ParameterDefaultValue("true")]
        public BooleanParameter MakeDefault = true;
        /// <summary>
        /// Whether to hide all the other printers.
        /// </summary>
        [ParameterDefaultValue("true")]
        public BooleanParameter HideOther = true;

        protected override bool AllowPreload => !Assigned(IdAndAppearance) || !IdAndAppearance.DynamicValue;
        protected override string AssignedId => !string.IsNullOrEmpty(IdAndAppearance?.Name) ? IdAndAppearance.Name : ActorManager.DefaultPrinterId;
        protected override string AlternativeAppearance => IdAndAppearance?.NamedValue;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            await base.ExecuteAsync(asyncToken);

            if (MakeDefault && !string.IsNullOrEmpty(AssignedId))
                ActorManager.DefaultPrinterId = AssignedId;

            if (HideOther)
                foreach (var printer in ActorManager.GetAllActors())
                    if (printer.Id != AssignedId && printer.Visible)
                        printer.ChangeVisibilityAsync(false, AssignedDuration, asyncToken: asyncToken).Forget();
        }
    }
}
