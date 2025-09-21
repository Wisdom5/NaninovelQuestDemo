// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Naninovel.Metadata;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows generating metadata to be used in external tools, eg IDE extension.
    /// </summary>
    public static class MetadataGenerator
    {
        /// <summary>
        /// Generates project-specific metadata (actors, resources, custom commands, etc).
        /// Doesn't include built-in commands, expression functions and constants.
        /// </summary>
        public static Project GenerateProjectMetadata ()
        {
            try
            {
                var meta = new Project();
                var customCommands = Command.CommandTypes.Values.Where(t => t.Namespace != Command.DefaultNamespace).ToList();
                var options = new MetadataOptions(customCommands, DisplayProgress, ResolveCustomCommandDocs, ResolveCustomParameterDocs);
                var providers = TypeCache.GetTypesDerivedFrom(typeof(IMetadataProvider)).Select(Activator.CreateInstance).Cast<IMetadataProvider>();
                foreach (var provider in providers)
                    meta = MergeMetadata(meta, provider.GetMetadata(options));
                return meta;
            }
            finally { EditorUtility.ClearProgressBar(); }

            void DisplayProgress (string info, float progress)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Generating Metadata", info, progress))
                    throw new OperationCanceledException("Metadata generation cancelled by the user.");
            }
        }

        /// <summary>
        /// Generates metadata for all the available commands, both built-in and custom.
        /// Doesn't include documentation.
        /// </summary>
        public static Metadata.Command[] GenerateCommandsMetadata ()
        {
            return GenerateCommandsMetadata(Command.CommandTypes.Values, _ => default, _ => null);
        }

        /// <summary>
        /// Generates metadata for the provided command types.
        /// </summary>
        /// <param name="commands">Command types to generate metadata for.</param>
        /// <param name="getCommandDoc">Function to retrieve documentation for command of the specified type.</param>
        /// <param name="getParamDoc">Function to retrieve documentation for parameter of the specified field.</param>
        public static Metadata.Command[] GenerateCommandsMetadata (IReadOnlyCollection<Type> commands,
            Func<Type, CommandDocumentation> getCommandDoc, Func<FieldInfo, string> getParamDoc)
        {
            var commandsMeta = new List<Metadata.Command>();
            foreach (var commandType in commands)
            {
                var commandDoc = getCommandDoc(commandType);
                var metadata = new Metadata.Command {
                    Id = commandType.Name,
                    Alias = ReflectionUtils.GetAttributeValue<Command.CommandAliasAttribute>(commandType, 0) as string,
                    Localizable = typeof(Command.ILocalizable).IsAssignableFrom(commandType),
                    Summary = commandDoc.Summary,
                    Remarks = commandDoc.Remarks,
                    Examples = commandDoc.Examples,
                    Parameters = GenerateParametersMetadata(commandType, getParamDoc)
                };
                commandsMeta.Add(metadata);
            }
            return commandsMeta.OrderBy(c => string.IsNullOrEmpty(c.Alias) ? c.Id : c.Alias).ToArray();
        }

        /// <summary>
        /// Generated constants metadata based on <see cref="ConstantContextAttribute"/> assigned to the commands
        /// of the provided types (enums only).
        /// </summary>
        public static Constant[] GenerateConstantsMetadata (IEnumerable<Type> commands)
        {
            var enumTypes = new HashSet<Type>();
            foreach (var command in commands)
            {
                if (command.GetCustomAttribute<ConstantContextAttribute>() is ConstantContextAttribute cmdAttr && cmdAttr.EnumType != null)
                    enumTypes.Add(cmdAttr.EnumType);
                foreach (var param in GetParameterFields(command))
                    if (param.GetCustomAttribute<ConstantContextAttribute>() is ConstantContextAttribute paramAttr && paramAttr.EnumType != null)
                        enumTypes.Add(paramAttr.EnumType);
            }
            var constants = new List<Constant>();
            foreach (var type in enumTypes)
                constants.Add(new Constant { Name = type.Name, Values = Enum.GetNames(type) });
            return constants.ToArray();
        }

        /// <summary>
        /// Generates metadata for the resources stored via editor provider.
        /// </summary>
        public static Metadata.Resource[] GenerateResourcesMetadata ()
        {
            var resources = new List<Metadata.Resource>();
            var editorResources = EditorResources.LoadOrDefault();
            var records = editorResources.GetAllRecords();
            foreach (var kv in records)
            {
                var record = editorResources.GetRecordByGuid(kv.Value);
                if (!record.HasValue) continue;
                var resource = new Metadata.Resource {
                    Type = record.Value.PathPrefix,
                    Path = record.Value.Name
                };
                resources.Add(resource);
            }
            return resources.ToArray();
        }

        /// <summary>
        /// Generates metadata for the actors stored via editor provider.
        /// </summary>
        public static Actor[] GenerateActorsMetadata ()
        {
            var actors = new List<Actor>();
            var editorResources = EditorResources.LoadOrDefault();
            var allResources = editorResources.GetAllRecords().Keys.ToArray();
            var chars = ProjectConfigurationProvider.LoadOrDefault<CharactersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in chars)
            {
                var charActor = new Actor {
                    Id = kv.Key,
                    Description = kv.Value.DisplayName,
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(charActor);
            }
            var backs = ProjectConfigurationProvider.LoadOrDefault<BackgroundsConfiguration>().Metadata.ToDictionary();
            foreach (var kv in backs)
            {
                var backActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(backActor);
            }
            var choiceHandlers = ProjectConfigurationProvider.LoadOrDefault<ChoiceHandlersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in choiceHandlers)
            {
                var choiceHandlerActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix
                };
                actors.Add(choiceHandlerActor);
            }
            var printers = ProjectConfigurationProvider.LoadOrDefault<TextPrintersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in printers)
            {
                var printerActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix
                };
                actors.Add(printerActor);
            }
            return actors.ToArray();

            string[] FindAppearances (string actorId, string pathPrefix, string actorImplementation)
            {
                var prefabPath = allResources.FirstOrDefault(p => p.EndsWithFast($"{pathPrefix}/{actorId}"));
                var assetGUID = prefabPath != null ? editorResources.GetGuidByPath(prefabPath) : null;
                var assetPath = assetGUID != null ? AssetDatabase.GUIDToAssetPath(assetGUID) : null;
                var prefabAsset = assetPath != null ? AssetDatabase.LoadMainAssetAtPath(assetPath) : null;
                if (prefabAsset != null && actorImplementation.Contains("Layered"))
                {
                    var layeredBehaviour = (prefabAsset as GameObject)?.GetComponent<LayeredActorBehaviour>();
                    return layeredBehaviour != null ? layeredBehaviour.GetCompositionMap().Keys.ToArray() : Array.Empty<string>();
                }
                else if (prefabAsset != null && (actorImplementation.Contains("Generic") ||
                                                 actorImplementation.Contains("Live2D") ||
                                                 actorImplementation.Contains("Spine")))
                {
                    var animator = (prefabAsset as GameObject)?.GetComponent<Animator>();
                    var controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                    return controller != null
                        ? controller.parameters.Where(p => p.type == AnimatorControllerParameterType.Trigger).Select(p => p.name).ToArray()
                        : Array.Empty<string>();
                }
                #if SPRITE_DICING_AVAILABLE
                else if (prefabAsset != null && actorImplementation.Contains("Diced"))
                {
                    return (prefabAsset as SpriteDicing.DicedSpriteAtlas)?.Sprites.Select(s => s.name).ToArray() ?? Array.Empty<string>();
                }
                #endif
                else
                {
                    var multiplePrefix = $"{pathPrefix}/{actorId}/";
                    return allResources.Where(p => p.Contains(multiplePrefix)).Select(p => p.GetAfter(multiplePrefix)).ToArray();
                }
            }
        }

        /// <summary>
        /// Generates metadata for custom variables assigned in configuration menu.
        /// </summary>
        public static string[] GenerateVariablesMetadata ()
        {
            var config = ProjectConfigurationProvider.LoadOrDefault<CustomVariablesConfiguration>();
            return config.PredefinedVariables.Select(p => p.Name).ToArray();
        }

        /// <summary>
        /// Generates metadata for custom expression functions (declared outside of Naninovel namespace).
        /// </summary>
        public static string[] GenerateFunctionsMetadata ()
        {
            return Engine.Types
                .Where(t => t.Namespace != typeof(ExpressionFunctions).Namespace && t.IsDefined(typeof(ExpressionFunctionsAttribute)))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)).Select(m => m.Name).Distinct().ToArray();
        }

        private static Parameter[] GenerateParametersMetadata (Type commandType, Func<FieldInfo, string> summaryResolver)
        {
            var result = new List<Parameter>();
            foreach (var fieldInfo in GetParameterFields(commandType))
                result.Add(ExtractParameterMetadata(fieldInfo, summaryResolver));
            return result.ToArray();
        }

        private static FieldInfo[] GetParameterFields (Type commandType)
        {
            return commandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(f => f.FieldType.GetInterface(nameof(ICommandParameter)) != null).ToArray();
        }

        private static Parameter ExtractParameterMetadata (FieldInfo field, Func<FieldInfo, string> summaryResolver)
        {
            var nullableName = typeof(INullable<>).Name;
            var namedName = typeof(INamed<>).Name;
            var meta = new Parameter {
                Id = field.Name,
                Alias = ReflectionUtils.GetAttributeValue<Command.ParameterAliasAttribute>(field, 0) as string,
                Required = ReflectionUtils.GetAttributeData<Command.RequiredParameterAttribute>(field) != null,
                Localizable = field.FieldType == typeof(LocalizableTextParameter),
                DefaultValue = ReflectionUtils.GetAttributeValue<Command.ParameterDefaultValueAttribute>(field, 0) as string,
                ValueContext = GetValueContext(field),
                Summary = summaryResolver(field)
            };
            meta.Nameless = meta.Alias == Command.NamelessParameterAlias;
            if (TryResolveValueType(field.FieldType, out var valueType))
                meta.ValueContainerType = ValueContainerType.Single;
            else if (GetInterface(nameof(IEnumerable)) != null) SetListValue();
            else SetNamedValue();
            meta.ValueType = valueType;
            return meta;

            Type GetInterface (string name) => field.FieldType.GetInterface(name);

            Type GetNullableType () => GetInterface(nullableName).GetGenericArguments()[0];

            void SetListValue ()
            {
                var elementType = GetNullableType().GetGenericArguments()[0];
                var namedElementType = elementType.BaseType?.GetGenericArguments()[0];
                if (namedElementType?.GetInterface(nameof(INamedValue)) != null)
                {
                    meta.ValueContainerType = ValueContainerType.NamedList;
                    var namedType = namedElementType.GetInterface(namedName).GetGenericArguments()[0];
                    TryResolveValueType(namedType, out valueType);
                }
                else
                {
                    meta.ValueContainerType = ValueContainerType.List;
                    TryResolveValueType(elementType, out valueType);
                }
            }

            void SetNamedValue ()
            {
                meta.ValueContainerType = ValueContainerType.Named;
                var namedType = GetNullableType().GetInterface(namedName).GetGenericArguments()[0];
                TryResolveValueType(namedType, out valueType);
            }
        }

        private static ValueContext[] GetValueContext (MemberInfo member)
        {
            var valueAttr = FindAttribute(false);
            if (valueAttr is null) return null;
            if (valueAttr is EndpointContextAttribute)
                return new[] {
                    new ValueContext { Type = ValueContextType.Resource, SubType = Constants.ScriptsType },
                    new ValueContext { Type = ValueContextType.Constant, SubType = EndpointResolver.BuildEndpointExpression(valueAttr.ParameterId ?? member.Name) }
                };
            return FindAttribute(true) is ParameterContextAttribute namedValueAttr
                ? new[] { GetValue(valueAttr), GetValue(namedValueAttr) }
                : new[] { GetValue(valueAttr) };

            ValueContext GetValue (ParameterContextAttribute attr) =>
                new ValueContext { Type = attr.Type, SubType = attr.SubType };
            ParameterContextAttribute FindAttribute (bool namedValue) =>
                member.MemberType == MemberTypes.Field ? FindFieldLevelContext(namedValue) : FindClassLevelContext(namedValue);
            ParameterContextAttribute FindClassLevelContext (bool namedValue) =>
                member.ReflectedType.GetCustomAttributes<ParameterContextAttribute>()
                    .Where(a => a.ParameterId == member.Name).FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            ParameterContextAttribute FindFieldLevelContext (bool namedValue) =>
                member.GetCustomAttributes<ParameterContextAttribute>().FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            bool OfSingleOrNamed (ParameterContextAttribute a, bool namedValue) => a.Index < 0 || a.Index == (namedValue ? 1 : 0);
        }

        private static bool TryResolveValueType (Type type, out Metadata.ValueType result)
        {
            var nullableName = typeof(INullable<>).Name;
            var valueTypeName = type.GetInterface(nullableName)?.GetGenericArguments()[0].Name;
            switch (valueTypeName)
            {
                case nameof(String):
                case nameof(NullableString):
                case nameof(LocalizableText):
                    result = Metadata.ValueType.String;
                    return true;
                case nameof(Int32):
                case nameof(NullableInteger):
                    result = Metadata.ValueType.Integer;
                    return true;
                case nameof(Single):
                case nameof(NullableFloat):
                    result = Metadata.ValueType.Decimal;
                    return true;
                case nameof(Boolean):
                case nameof(NullableBoolean):
                    result = Metadata.ValueType.Boolean;
                    return true;
            }
            result = default;
            return false;
        }

        private static CommandDocumentation ResolveCustomCommandDocs (Type type)
        {
            var summary = ReflectionUtils.GetAttributeValue<DocumentationAttribute>(type, 0) as string;
            var remarks = ReflectionUtils.GetAttributeValue<DocumentationAttribute>(type, 1) as string;
            return new CommandDocumentation(summary, remarks, null);
        }

        private static string ResolveCustomParameterDocs (FieldInfo field)
        {
            return ReflectionUtils.GetAttributeValue<DocumentationAttribute>(field, 0) as string;
        }

        private static Project MergeMetadata (Project meta1, Project meta2)
        {
            return new Project {
                Actors = meta1.Actors.Concat(meta2.Actors).ToArray(),
                Commands = meta1.Commands.Concat(meta2.Commands).ToArray(),
                Constants = meta1.Constants.Concat(meta2.Constants).ToArray(),
                Functions = meta1.Functions.Concat(meta2.Functions).ToArray(),
                Resources = meta1.Resources.Concat(meta2.Resources).ToArray(),
                Variables = meta1.Variables.Concat(meta2.Variables).ToArray()
            };
        }
    }
}
