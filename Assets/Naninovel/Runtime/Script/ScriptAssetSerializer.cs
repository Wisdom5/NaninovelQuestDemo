// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Naninovel.Commands;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <summary>
    /// Allows serializing <see cref="Script"/> asset into script text.
    /// </summary>
    public class ScriptAssetSerializer
    {
        private readonly ScriptSerializer scriptSerializer = new ScriptSerializer();
        private readonly ListValueSerializer listSerializer = new ListValueSerializer();
        private readonly NamedValueSerializer namedSerializer = new NamedValueSerializer();
        private readonly StringBuilder builder = new StringBuilder();
        private readonly List<IValueComponent> components = new List<IValueComponent>();
        private ScriptTextMap map;

        public string Serialize (Script script)
        {
            Reset(script.TextMap);
            foreach (var line in script.Lines)
            {
                AppendLine(line);
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public string Serialize (ScriptLine line, ScriptTextMap map)
        {
            Reset(map);
            AppendLine(line);
            return builder.ToString();
        }

        private void Reset (ScriptTextMap map)
        {
            builder.Clear();
            this.map = map;
        }

        private void AppendLine (ScriptLine line)
        {
            if (line is EmptyScriptLine empty) AppendEmptyLine(empty);
            if (line is CommentScriptLine comment) AppendCommentLine(comment);
            if (line is LabelScriptLine label) AppendLabelLine(label);
            if (line is CommandScriptLine cmd) AppendCommandLine(cmd);
            if (line is GenericTextScriptLine generic) AppendGenericLine(generic);
        }

        private void AppendEmptyLine (EmptyScriptLine _)
        {
            builder.Append('\n');
        }

        private void AppendCommentLine (CommentScriptLine line)
        {
            builder.Append(Identifiers.CommentLine).Append(' ').Append(line.CommentText);
        }

        private void AppendLabelLine (LabelScriptLine line)
        {
            builder.Append(Identifiers.LabelLine).Append(' ').Append(line.LabelText);
        }

        private void AppendCommandLine (CommandScriptLine line)
        {
            builder.Append(Identifiers.CommandLine);
            AppendCommand(line.Command);
        }

        private void AppendGenericLine (GenericTextScriptLine line)
        {
            AppendPrefix();
            var shouldSkipFirst = line.InlinedCommands.FirstOrDefault() is ModifyCharacter;
            for (int i = shouldSkipFirst ? 1 : 0; i < line.InlinedCommands.Count; i++)
                AppendInlined(line.InlinedCommands[i]);

            void AppendPrefix ()
            {
                if (line.InlinedCommands.FirstOrDefault() is ModifyCharacter mod)
                    AppendPrefixFromModification(mod);
                else FindAndAppendAuthorId();
            }

            void AppendPrefixFromModification (ModifyCharacter mod)
            {
                AppendValue(mod.Id, default);
                builder.Append(Identifiers.AuthorAppearance);
                AppendValue(mod.Appearance, default);
                builder.Append(Identifiers.AuthorAssign);
            }

            void FindAndAppendAuthorId ()
            {
                var authored = line.InlinedCommands.OfType<PrintText>().FirstOrDefault(c => Command.Assigned(c.AuthorId));
                if (authored == null) return;
                AppendValue(authored.AuthorId, default);
                builder.Append(Identifiers.AuthorAssign);
            }

            void AppendInlined (Command inlined)
            {
                if (inlined is WaitForInput && line.InlinedCommands.LastOrDefault() == inlined) return;
                if (inlined is PrintText print) AppendGenericText(print);
                else
                {
                    builder.Append(Identifiers.InlinedOpen);
                    AppendCommand(inlined);
                    builder.Append(Identifiers.InlinedClose);
                }
            }

            void AppendGenericText (PrintText print)
            {
                AppendValue(print.Text, new SerializationContext {
                    FirstGenericContent = line.InlinedCommands.IndexOf(print) == 0 && !Command.Assigned(print.AuthorId)
                });
                var appendWait = print.WaitForInput && line.InlinedCommands.LastOrDefault() != print;
                if (appendWait) builder.Append("[i]");
            }
        }

        private void AppendCommand (Command command)
        {
            var commandId = Command.CommandTypes.First(kv => kv.Value == command.GetType()).Key.FirstToLower();
            var parameters = CommandParameter.Extract(command);
            builder.Append(commandId);
            for (var i = 0; i < parameters.Count; i++)
                AppendParameter(parameters[i], i == parameters.Count - 1);

            void AppendParameter (ParameterInfo info, bool last)
            {
                if (!info.Instance.HasValue) return;
                var value = SerializeValue(info.Instance, new SerializationContext { ParameterValue = true });
                if (info.DefaultValue?.EqualsFastIgnoreCase(value) ?? false) return;
                builder.Append(' ');
                var id = info.Alias ?? info.Id.FirstToLower();
                if (id != Command.NamelessParameterAlias)
                    builder.Append(id).Append(Identifiers.ParameterAssign);
                builder.Append(value);
            }
        }

        private void AppendValue (ICommandParameter param, SerializationContext context)
        {
            builder.Append(SerializeValue(param, context));
        }

        private string SerializeValue (ICommandParameter param, SerializationContext context)
        {
            if (param.RawValue.HasValue) return SerializeRaw(param.RawValue.Value, context);
            if (param is StringParameter str) return SerializeString(str.Value);
            if (param is LocalizableTextParameter text) return SerializeLocalizableText(text.Value);
            if (param is BooleanParameter boolean) return SerializeBool(boolean.Value);
            if (param is DecimalParameter dec) return SerializeDecimal(dec.Value);
            if (param is IntegerParameter integer) return SerializeInteger(integer.Value);
            if (param is NamedStringParameter namedString) return SerializeNamedString(namedString.Value);
            if (param is NamedBooleanParameter namedBoolean) return SerializeNamedBoolean(namedBoolean.Value);
            if (param is NamedDecimalParameter namedDecimal) return SerializeNamedDecimal(namedDecimal.Value);
            if (param is NamedIntegerParameter namedInteger) return SerializeNamedInteger(namedInteger.Value);
            if (param is StringListParameter stringList) return BuildList(stringList, SerializeString);
            if (param is BooleanListParameter booleanList) return BuildList(booleanList, SerializeBool);
            if (param is DecimalListParameter decimalList) return BuildList(decimalList, SerializeDecimal);
            if (param is IntegerListParameter integerList) return BuildList(integerList, SerializeInteger);
            if (param is NamedStringListParameter namedStringList) return BuildList(namedStringList, SerializeNamedString);
            if (param is NamedBooleanListParameter namedBooleanList) return BuildList(namedBooleanList, SerializeNamedBoolean);
            if (param is NamedDecimalListParameter namedDecimalList) return BuildList(namedDecimalList, SerializeNamedDecimal);
            if (param is NamedIntegerListParameter namedIntegerList) return BuildList(namedIntegerList, SerializeNamedInteger);
            return "";

            string SerializeString (NullableString value) => !value.HasValue ? "" : value.Value;
            string SerializeLocalizableText (LocalizableText value) => value.ToString();
            string SerializeBool (NullableBoolean value) => !value.HasValue ? "" : value.Value ? "true" : "false";
            string SerializeDecimal (NullableFloat value) => !value.HasValue ? "" : value.Value.ToString(CultureInfo.InvariantCulture);
            string SerializeInteger (NullableInteger value) => !value.HasValue ? "" : value.Value.ToString(CultureInfo.InvariantCulture);
            string SerializeNamedString (NullableNamedString value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeString(value.Value.Value));
            string SerializeNamedBoolean (NullableNamedBoolean value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeBool(value.Value.Value));
            string SerializeNamedDecimal (NullableNamedFloat value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeDecimal(value.Value.Value));
            string SerializeNamedInteger (NullableNamedInteger value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeInteger(value.Value.Value));

            string BuildNamed (string name, string value)
            {
                if (string.IsNullOrEmpty(name)) name = null;
                if (string.IsNullOrEmpty(value)) value = null;
                return namedSerializer.Serialize(name, value);
            }

            string BuildList<T> (IEnumerable<T> items, Func<T, string> serializeItem)
            {
                var serializedItems = items.Select(serializeItem).Select(v => string.IsNullOrEmpty(v) ? null : v);
                return listSerializer.Serialize(serializedItems.ToArray());
            }
        }

        private string SerializeRaw (RawValue raw, SerializationContext context)
        {
            components.Clear();
            foreach (var part in raw.Parts)
                if (part.Kind == ParameterValuePartKind.IdentifiedText)
                    components.Add(new IdentifiedText(new PlainText(map.GetTextOrNull(part.Id)), new TextIdentifier(part.Id)));
                else if (part.Kind == ParameterValuePartKind.Expression)
                    components.Add(new Expression(new PlainText(part.Expression)));
                else components.Add(new PlainText(part.Text));
            return scriptSerializer.Serialize(components, context);
        }
    }
}
