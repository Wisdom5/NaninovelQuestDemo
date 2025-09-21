// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Naninovel.Parsing;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="CommandParameter"/>
    public static class CommandParameter
    {
        /// <inheritdoc cref="ICommandParameter.AssignRaw"/>
        public static T FromRaw<T> (RawValue? raw, PlaybackSpot? spot, out string errors) where T : ICommandParameter, new()
        {
            var parameter = new T();
            parameter.AssignRaw(raw, spot, out errors);
            return parameter;
        }

        /// <summary>
        /// Extracts all parameters contained in specified command.
        /// </summary>
        public static IReadOnlyList<ParameterInfo> Extract (Command command)
        {
            var parameters = new List<ParameterInfo>();
            var fields = command.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var instance = field.GetValue(command) as ICommandParameter;
                if (instance is null) continue;
                var id = field.Name;
                var alias = field.GetCustomAttribute<Command.ParameterAliasAttribute>()?.Alias;
                var defaultValue = field.GetCustomAttribute<Command.ParameterDefaultValueAttribute>()?.Value;
                var required = field.GetCustomAttribute<Command.RequiredParameterAttribute>() != null;
                parameters.Add(new ParameterInfo(instance, id, alias, defaultValue, required));
            }
            return parameters;
        }
    }

    /// <summary>
    /// Represents a <see cref="Command"/> parameter.
    /// </summary>
    /// <typeparam name="TValue">Type of the parameter value; should be natively supported by the Unity serialization system.</typeparam>
    [Serializable]
    public abstract class CommandParameter<TValue> : Nullable<TValue>, ICommandParameter
    {
        public bool DynamicValue => RawValue?.Dynamic ?? false;
        public PlaybackSpot? PlaybackSpot => spot == Naninovel.PlaybackSpot.Invalid ? (PlaybackSpot?)null : spot;
        public RawValue? RawValue => raw.Parts.Count == 0 ? (RawValue?)null : raw;

        private readonly NamedValueParser parser = new NamedValueParser();

        [SerializeField] private RawValue raw;
        [SerializeField] private PlaybackSpot spot;

        public override string ToString () => RawValue?.ToString() ?? base.ToString();

        public virtual void AssignRaw (RawValue? raw, PlaybackSpot? spot, out string errors)
        {
            this.raw = raw ?? default;
            this.spot = spot ?? Naninovel.PlaybackSpot.Invalid;
            errors = null;
            base.SetValue(DynamicValue ? default : ParseRaw(this.raw, out errors));
            if (DynamicValue) HasValue = true;
        }

        protected override TValue GetValue () => DynamicValue ? EvaluateDynamicValue() : base.GetValue();

        protected override void SetValue (TValue value)
        {
            raw = default;
            base.SetValue(value);
        }

        protected virtual TValue EvaluateDynamicValue ()
        {
            if (!DynamicValue) throw new Error($"Failed to evaluate dynamic value of `{GetType().Name}` command parameter: the value is not dynamic. {spot}");
            if (!(Engine.Behaviour is RuntimeBehaviour)) throw new Error($"Attempting to evaluate dynamic value of `{GetType().Name}` command parameter while the engine is not initialized. {spot}");
            var parts = new RawValuePart[raw.Parts.Count];
            for (int i = 0; i < raw.Parts.Count; i++)
                if (raw.Parts[i].Kind == ParameterValuePartKind.Expression)
                    parts[i] = RawValuePart.FromPlainText(ExpressionEvaluator.Evaluate<string>(raw.Parts[i].Expression, LogEvaluationError));
                else parts[i] = raw.Parts[i];
            var value = ParseRaw(new RawValue(parts), out var errors);
            if (!string.IsNullOrEmpty(errors)) Engine.Err(errors, spot);
            return value;

            void LogEvaluationError (string message) => Engine.Err(message, spot);
        }

        protected virtual string InterpolatePlainText (IReadOnlyList<RawValuePart> parts)
        {
            if (parts.Count == 1 && parts[0].Kind == ParameterValuePartKind.PlainText)
                return parts[0].Text;

            var builder = new StringBuilder();
            foreach (var part in parts)
                if (part.Kind != ParameterValuePartKind.PlainText)
                    Engine.Warn($"Unsupported parameter value part. Make sure you're not assigning text ID to an unsupported parameter or otherwise using `{Identifiers.TextIdOpen}` w/o escaping.", spot);
                else builder.Append(part.Text);
            return builder.ToString();
        }

        protected virtual int ParseIntegerText (string intText, out string errors)
        {
            errors = ParseUtils.TryInvariantInt(intText, out var result) ? null : $"Failed to parse `{intText}` string into `{nameof(Int32)}`";
            return result;
        }

        protected virtual float ParseFloatText (string floatText, out string errors)
        {
            errors = ParseUtils.TryInvariantFloat(floatText, out var result) ? null : $"Failed to parse `{floatText}` string into `{nameof(Single)}`";
            return result;
        }

        protected virtual bool ParseBooleanText (string boolText, out string errors)
        {
            errors = bool.TryParse(boolText, out var result) ? null : $"Failed to parse `{boolText}` string into `{nameof(Boolean)}`";
            return result;
        }

        protected virtual void ParseNamedValueText (string valueText, out string name, out string value, out string errors)
        {
            errors = null;
            (name, value) = parser.Parse(valueText);
        }

        protected abstract TValue ParseRaw (RawValue raw, out string errors);
    }
}
