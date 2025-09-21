// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Naninovel.Parsing;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommandLineView : ScriptLineView
    {
        private struct ParameterFieldData
        {
            public LineTextField Field;
            public string Id, Value;
            public bool Nameless;
        }

        public string CommandId { get; private set; }

        private static readonly ErrorCollector errors = new ErrorCollector();
        private static readonly Parsing.CommandLineParser commandLineParser = new Parsing.CommandLineParser(new ParseHandlers { ErrorHandler = errors });
        private static readonly Metadata.Command[] metadata = MetadataGenerator.GenerateCommandsMetadata();
        private static readonly ScriptSerializer serializer = new ScriptSerializer();

        private readonly List<ParameterFieldData> parameterFields = new List<ParameterFieldData>();
        private readonly List<ParameterFieldData> delayedAddFields = new List<ParameterFieldData>();

        private bool hideParameters;

        private CommandLineView (int lineIndex, VisualElement container)
            : base(lineIndex, container) { }

        public static ScriptLineView CreateDefault (int lineIndex, string commandId,
            VisualElement container, bool hideParameters)
        {
            var lineText = $"{Identifiers.CommandLine}{commandId}";
            var tokens = new List<Token>();
            new Lexer().TokenizeLine(lineText, tokens);
            return CreateOrError(lineIndex, lineText, tokens, container, hideParameters);
        }

        public static ScriptLineView CreateOrError (int lineIndex, string lineText,
            IReadOnlyList<Token> tokens, VisualElement container, bool hideParameters)
        {
            errors.Clear();
            var model = commandLineParser.Parse(lineText, tokens).Command;
            if (errors.Count > 0) return Error(errors.FirstOrDefault()?.Message);

            var commandId = model.Identifier.Text;
            var meta = metadata.FirstOrDefault(c => c.Id.EqualsFastIgnoreCase(commandId) ||
                                                    (c.Alias?.EqualsFastIgnoreCase(commandId) ?? false));
            if (meta is null) return Error($"Unknown command: `{commandId}`");

            var nameLabel = new Label(commandId.FirstToLower());
            nameLabel.name = "InputLabel";
            nameLabel.AddToClassList("Inlined");

            var commandLineView = new CommandLineView(lineIndex, container);
            commandLineView.Content.Add(nameLabel);
            commandLineView.CommandId = commandId;
            commandLineView.hideParameters = hideParameters;

            foreach (var paramMeta in meta.Parameters)
            {
                var data = new ParameterFieldData {
                    Id = string.IsNullOrEmpty(paramMeta.Alias) ? paramMeta.Id.FirstToLower() : paramMeta.Alias,
                    Value = GetValueFor(paramMeta),
                    Nameless = paramMeta.Nameless
                };
                if (commandLineView.ShouldShowParameter(data))
                    commandLineView.AddParameterField(data);
                else commandLineView.delayedAddFields.Add(data);
            }

            return commandLineView;

            ErrorLineView Error (string e) => new ErrorLineView(lineIndex, lineText, container, model.Identifier, e);

            string GetValueFor (Metadata.Parameter m)
            {
                var param = model.Parameters.FirstOrDefault(p => p.Nameless && m.Nameless || p.Identifier != null &&
                    (p.Identifier.Text.EqualsFastIgnoreCase(m.Id) || p.Identifier.Text.EqualsFastIgnoreCase(m.Alias)));
                if (param is null) return null;
                return serializer.Serialize(param.Value, new SerializationContext { ParameterValue = true });
            }
        }

        public override string GenerateLineText ()
        {
            var result = $"{Identifiers.CommandLine}{CommandId}";
            if (parameterFields.Any(f => f.Nameless))
                result += $" {EncodeValue(parameterFields.First(f => f.Nameless))}";
            foreach (var data in parameterFields)
                if (!string.IsNullOrEmpty(data.Field.label) && !string.IsNullOrWhiteSpace(data.Field.value))
                    result += $" {data.Id}:{EncodeValue(data)}";
            return result;

            string EncodeValue (ParameterFieldData data)
            {
                return data.Field?.value;
            }
        }

        protected override void ApplyFocusedStyle ()
        {
            base.ApplyFocusedStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotFocusedStyle ()
        {
            base.ApplyNotFocusedStyle();

            HideUnAssignedNamedFields();
        }

        protected override void ApplyHoveredStyle ()
        {
            base.ApplyHoveredStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotHoveredStyle ()
        {
            base.ApplyNotHoveredStyle();

            if (FocusedLine == this) return;
            HideUnAssignedNamedFields();
        }

        private void AddParameterField (ParameterFieldData data)
        {
            data.Field = new LineTextField(data.Nameless ? "" : data.Id, data.Value ?? "");
            if (!data.Nameless) data.Field.AddToClassList("NamedParameterLabel");
            parameterFields.Add(data);
            if (ShouldShowParameter(data)) Content.Add(data.Field);
        }

        private bool ShouldShowParameter (ParameterFieldData data)
        {
            return !hideParameters || data.Nameless || !string.IsNullOrEmpty(data.Value);
        }

        private void ShowUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            // Add un-assigned fields in case they weren't added on init.
            if (delayedAddFields.Count > 0)
            {
                foreach (var data in delayedAddFields)
                    AddParameterField(data);
                delayedAddFields.Clear();
            }

            foreach (var data in parameterFields)
                if (!Content.Contains(data.Field))
                    Content.Add(data.Field);
        }

        private void HideUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            foreach (var data in parameterFields)
                if (!string.IsNullOrEmpty(data.Field.label)
                    && string.IsNullOrWhiteSpace(data.Field.value)
                    && Content.Contains(data.Field))
                    Content.Remove(data.Field);
        }
    }
}
