// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Naninovel.ManagedText;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class ManagedTextWindow : EditorWindow
    {
        protected string OutputPath
        {
            get => PlayerPrefs.GetString(outputPathKey, $"{Application.dataPath}/Resources/Naninovel/{ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}");
            set
            {
                PlayerPrefs.SetString(outputPathKey, value);
                ValidateOutputPath();
            }
        }

        private static readonly GUIContent outputPathContent = new GUIContent("Output Path", "Path to the folder under which to sore generated managed text documents; should be `Resources/Naninovel/Text` by default.");
        private static readonly GUIContent deleteUnusedContent = new GUIContent("Delete Unused", "Whether to delete documents that doesn't correspond to any static fields with `ManagedTextAttribute`.");

        private const string outputPathKey = "Naninovel." + nameof(ManagedTextWindow) + "." + nameof(OutputPath);
        private bool isWorking;
        private bool deleteUnused;
        private bool outputPathValid;
        private string pathPrefix;

        [MenuItem("Naninovel/Tools/Managed Text")]
        public static void OpenWindow ()
        {
            var position = new Rect(100, 100, 500, 135);
            GetWindowWithRect<ManagedTextWindow>(position, true, "Managed Text", true);
        }

        private void OnEnable ()
        {
            ValidateOutputPath();
        }

        private void ValidateOutputPath ()
        {
            pathPrefix = ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().Loader.PathPrefix;
            outputPathValid = OutputPath?.EndsWith(pathPrefix) ?? false;
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField("Naninovel Managed Text", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The tool to generate managed text documents; see `Managed Text` guide for usage instructions.", EditorStyles.miniLabel);

            EditorGUILayout.Space();

            if (isWorking)
            {
                EditorGUILayout.HelpBox("Working, please wait...", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                OutputPath = EditorGUILayout.TextField(outputPathContent, OutputPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    OutputPath = EditorUtility.OpenFolderPanel("Output Path", "", "");
            }
            deleteUnused = EditorGUILayout.Toggle(deleteUnusedContent, deleteUnused);

            GUILayout.FlexibleSpace();

            if (!outputPathValid)
                EditorGUILayout.HelpBox($"Output path is not valid. Make sure it points to a `{pathPrefix}` folder stored under a `Resources` folder.", MessageType.Error);
            else if (GUILayout.Button("Generate Managed Text Documents", GUIStyles.NavigationButton))
                GenerateDocuments();
            EditorGUILayout.Space();
        }

        private void GenerateDocuments ()
        {
            isWorking = true;

            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);

            var categoryToRecords = GenerateRecordsGroupedByCategory();

            foreach (var kv in categoryToRecords)
                ProcessDocumentCategory(kv.Key, kv.Value);

            if (deleteUnused)
                DeleteUnusedDocuments(categoryToRecords.Keys.ToList());

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            isWorking = false;
            Repaint();
        }

        private void ProcessDocumentCategory (string category, HashSet<ManagedTextRecord> records)
        {
            var fullPath = $"{OutputPath}/{category}.txt";

            // Try to update existing resource.
            if (File.Exists(fullPath))
            {
                var documentText = File.ReadAllText(fullPath);
                var existingRecords = new HashSet<ManagedTextRecord>(ManagedTextUtils.Parse(documentText, category).Records);
                // Remove existing fields no longer associated with the category (possibly moved to another or deleted).
                existingRecords.RemoveWhere(t => !records.Contains(t));
                // Remove new fields that already exist in the updated document, to prevent overriding.
                records.ExceptWith(existingRecords);
                // Add existing fields to the new set.
                records.UnionWith(existingRecords);
                File.Delete(fullPath);
            }

            var document = new ManagedTextDocument(records.OrderBy(r => r.Key));
            File.WriteAllText(fullPath, ManagedTextUtils.Serialize(document, category, 0));
        }

        private void DeleteUnusedDocuments (List<string> usedCategories)
        {
            usedCategories.Add(ManagedTextConfiguration.TipCategory);
            usedCategories.Add(ExpressionEvaluator.ManagedTextScriptCategory);
            foreach (var filePath in Directory.EnumerateFiles(OutputPath, "*.txt"))
                if (!usedCategories.Contains(Path.GetFileName(filePath).GetBeforeLast(".txt")))
                    File.Delete(filePath);
        }

        private static Dictionary<string, HashSet<ManagedTextRecord>> GenerateRecordsGroupedByCategory ()
        {
            var map = new Dictionary<string, HashSet<ManagedTextRecord>>();
            AddStaticFields(map);
            AddDisplayNames(map);
            AddPrefabs(map);
            AddLocales(map);
            return map;
        }

        private static void AddStaticFields (Dictionary<string, HashSet<ManagedTextRecord>> map)
        {
            foreach (var fieldInfo in Engine.Types
                         .SelectMany(type => type.GetFields(ManagedTextUtils.ManagedFieldBindings))
                         .Where(field => field.IsDefined(typeof(ManagedTextAttribute))))
                AddFromFieldInfo(fieldInfo);

            void AddFromFieldInfo (FieldInfo fieldInfo)
            {
                var attribute = fieldInfo.GetCustomAttribute<ManagedTextAttribute>();
                var fieldId = $"{fieldInfo.ReflectedType}.{fieldInfo.Name}";
                var fieldValue = fieldInfo.GetValue(null) as string;
                GetOrAddCategory(map, attribute.Category).Add(new ManagedTextRecord(fieldId, fieldValue));
            }
        }

        private static void AddDisplayNames (Dictionary<string, HashSet<ManagedTextRecord>> map)
        {
            var records = new HashSet<ManagedTextRecord>();
            var charConfig = ProjectConfigurationProvider.LoadOrDefault<CharactersConfiguration>();
            foreach (var kv in charConfig.Metadata.ToDictionary())
                records.Add(new ManagedTextRecord(kv.Key, kv.Value.DisplayName));
            map[CharactersConfiguration.DisplayNamesCategory] = records;
        }

        private static void AddPrefabs (Dictionary<string, HashSet<ManagedTextRecord>> map)
        {
            var providers = new List<ManagedTextProvider>();
            var editorResources = EditorResources.LoadOrDefault();
            var uiConfig = ProjectConfigurationProvider.LoadOrDefault<UIConfiguration>();
            foreach (var kv in editorResources.GetAllRecords(uiConfig.UILoader.PathPrefix))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(kv.Value);
                if (assetPath is null) continue; // UI with a non-valid resource.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                ProcessPrefab(prefab);
            }

            foreach (var kv in ProjectConfigurationProvider.LoadOrDefault<TextPrintersConfiguration>().Metadata.ToDictionary())
                ProcessActor<ITextPrinterActor>(kv.Key, kv.Value);
            foreach (var kv in ProjectConfigurationProvider.LoadOrDefault<ChoiceHandlersConfiguration>().Metadata.ToDictionary())
                ProcessActor<IChoiceHandlerActor>(kv.Key, kv.Value);

            void ProcessPrefab (GameObject prefab)
            {
                if (!ObjectUtils.IsValid(prefab)) return;
                prefab.GetComponentsInChildren(true, providers);
                providers.ForEach(p => GetOrAddCategory(map, p.Category).Add(new ManagedTextRecord(p.Key, p.DefaultValue)));
                providers.Clear();
            }

            void ProcessActor<TActor> (string id, ActorMetadata meta) where TActor : IActor
            {
                if (!typeof(TActor).IsAssignableFrom(Type.GetType(meta.Implementation))) return;
                var resourcePath = $"{meta.Loader.PathPrefix}/{id}";
                var guid = editorResources.GetGuidByPath(resourcePath);
                if (guid is null) return; // Actor without an assigned resource.
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath is null) return; // Actor with a non-valid resource.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                ProcessPrefab(prefab);
            }
        }

        private static void AddLocales (Dictionary<string, HashSet<ManagedTextRecord>> map)
        {
            var records = new HashSet<ManagedTextRecord>();
            foreach (var lang in Languages.GetAll())
                records.Add(new ManagedTextRecord(lang.Tag, lang.Name));
            map[Languages.ManagedTextCategory] = records;
        }

        private static HashSet<ManagedTextRecord> GetOrAddCategory (Dictionary<string, HashSet<ManagedTextRecord>> map, string category)
        {
            if (string.IsNullOrEmpty(category)) category = ManagedTextConfiguration.DefaultCategory;
            if (map.TryGetValue(category, out var result)) return result;
            return map[category] = new HashSet<ManagedTextRecord>();
        }
    }
}
