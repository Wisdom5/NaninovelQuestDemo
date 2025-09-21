// Copyright 2023 ReWaffle LLC. All rights reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class VoiceoverWindow : EditorWindow
    {
        public enum Format
        {
            Plaintext,
            Markdown,
            CSV
        }

        protected string OutputPath { get => PlayerPrefs.GetString(outputPathKey); set => PlayerPrefs.SetString(outputPathKey, value); }
        protected Format OutputFormat { get => (Format)PlayerPrefs.GetInt(outputFormatKey); set => PlayerPrefs.SetInt(outputFormatKey, (int)value); }

        private static readonly GUIContent localeLabel = new GUIContent("Locale");
        private static readonly GUIContent formatLabel = new GUIContent("Format",
            "Type of file and formatting of the voiceover documents to produce:" +
            "\n • Plaintext — .txt file without any formatting." +
            "\n • Markdown — .md file with additional markdown for better readability." +
            "\n • CSV — .csv file with comma-separated values to be used with table processors, such as Google Sheets or Microsoft Excel.");

        private const string outputPathKey = "Naninovel." + nameof(VoiceoverWindow) + "." + nameof(OutputPath);
        private const string outputFormatKey = "Naninovel." + nameof(VoiceoverWindow) + "." + nameof(OutputFormat);

        private static readonly IVoiceoverDocumentGenerator customGenerator = GetCustomGenerator();
        private bool isWorking;
        private IScriptManager scripts;
        private ILocalizationManager l10n;
        private ITextManager text;
        private string locale;

        [MenuItem("Naninovel/Tools/Voiceover Documents")]
        public static void OpenWindow ()
        {
            var position = new Rect(100, 100, 500, 160);
            GetWindowWithRect<VoiceoverWindow>(position, true, "Voiceover Documents", true);
        }

        private void OnEnable ()
        {
            if (!Engine.Initialized)
            {
                isWorking = true;
                Engine.OnInitializationFinished += InitializeEditor;
                EditorInitializer.InitializeAsync().Forget();
            }
            else InitializeEditor();
        }

        private void OnDisable ()
        {
            Engine.Destroy();
        }

        private void InitializeEditor ()
        {
            Engine.OnInitializationFinished -= InitializeEditor;

            scripts = Engine.GetService<IScriptManager>();
            l10n = Engine.GetService<ILocalizationManager>();
            text = Engine.GetService<ITextManager>();
            locale = ProjectConfigurationProvider.LoadOrDefault<LocalizationConfiguration>().SourceLocale;
            isWorking = false;
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField("Naninovel Voiceover Documents", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The tool to generate voiceover documents; see `Voicing` guide for usage instructions.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            if (isWorking)
            {
                EditorGUILayout.HelpBox("Working, please wait...", MessageType.Info);
                return;
            }

            locale = LocalesPopupDrawer.Draw(locale, localeLabel);
            if (customGenerator != null) EditorGUILayout.LabelField("Custom Generator", customGenerator.GetType().Name);
            else OutputFormat = (Format)EditorGUILayout.EnumPopup(formatLabel, OutputFormat);
            using (new EditorGUILayout.HorizontalScope())
            {
                OutputPath = EditorGUILayout.TextField("Output Path", OutputPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    OutputPath = EditorUtility.OpenFolderPanel("Output Path", "", "");
            }

            GUILayout.FlexibleSpace();

            if (!l10n.LocaleAvailable(locale))
                EditorGUILayout.HelpBox($"Selected locale is not available. Make sure a `{locale}` directory exists in the localization resources.", MessageType.Warning, true);
            else
            {
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(OutputPath));
                if (GUILayout.Button("Generate Voiceover Documents", GUIStyles.NavigationButton))
                    GenerateVoiceoverDocumentsAsync().Forget();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.Space();
        }

        private async UniTask GenerateVoiceoverDocumentsAsync ()
        {
            try
            {
                isWorking = true;

                EditorUtility.DisplayProgressBar("Generating Voiceover Documents", "Initializing...", 0f);

                await l10n.SelectLocaleAsync(locale);

                WriteVoiceoverDocuments(scripts.Scripts.ToList());

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                isWorking = false;
                Repaint();

                EditorUtility.ClearProgressBar();
            }
            catch (Exception e)
            {
                Engine.Err($"Failed to generate voiceover documents: {e}");
            }
            finally
            {
                isWorking = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private void WriteVoiceoverDocuments (List<Script> scripts)
        {
            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);

            var outputDir = new DirectoryInfo(OutputPath);
            if (CleanDirectoryPrompt.PromptIfNotEmpty(outputDir))
                outputDir.GetFiles().ToList().ForEach(f => f.Delete());
            else throw new Error("Operation canceled by user.");

            for (int i = 0; i < scripts.Count; i++)
            {
                var script = new ScriptPlaylist(scripts[i]);
                var progress = i / (float)scripts.Count;
                EditorUtility.DisplayProgressBar("Generating Voiceover Documents", $"Processing `{script.ScriptName}` script...", progress);

                if (customGenerator != null)
                    customGenerator.GenerateVoiceoverDocument(script, locale ?? "default", OutputPath);
                else if (OutputFormat == Format.Plaintext) GeneratePlainText(script);
                else if (OutputFormat == Format.Markdown) GenerateMarkdown(script);
                else if (OutputFormat == Format.CSV) GenerateCSV(script);
            }
        }

        private static IVoiceoverDocumentGenerator GetCustomGenerator ()
        {
            var type = TypeCache.GetTypesDerivedFrom<IVoiceoverDocumentGenerator>().FirstOrDefault();
            if (type is null) return null;
            return Activator.CreateInstance(type) as IVoiceoverDocumentGenerator;
        }

        private void GeneratePlainText (ScriptPlaylist script)
        {
            var builder = new StringBuilder($"Voiceover document for script '{script.ScriptName}' ({locale ?? "default"} locale)\n\n");
            foreach (var cmd in script.OfType<PrintText>())
            {
                var spot = AudioConfiguration.GetAutoVoiceClipPath(cmd.PlaybackSpot);
                var id = AudioConfiguration.GetAutoVoiceClipPath(cmd.Text);
                builder.Append($"{spot} #{id}\n");
                if (Command.Assigned(cmd.AuthorId))
                    builder.Append($"{cmd.AuthorId}: ");
                builder.Append($"{ResolveText(script.ScriptName, cmd.Text)}\n\n");
            }
            File.WriteAllText($"{OutputPath}/{script.ScriptName}.txt", builder.ToString());
        }

        private void GenerateMarkdown (ScriptPlaylist script)
        {
            var builder = new StringBuilder($"# Voiceover document for script '{script.ScriptName}' ({locale ?? "default"} locale)\n\n");
            foreach (var cmd in script.OfType<PrintText>())
            {
                var spot = AudioConfiguration.GetAutoVoiceClipPath(cmd.PlaybackSpot);
                var id = AudioConfiguration.GetAutoVoiceClipPath(cmd.Text);
                builder.Append($"## {spot} #`{id}`\n");
                if (Command.Assigned(cmd.AuthorId))
                    builder.Append($"**{cmd.AuthorId}**: ");
                builder.Append($"{ResolveText(script.ScriptName, cmd.Text)}\n\n");
            }
            File.WriteAllText($"{OutputPath}/{script.ScriptName}.md", builder.ToString());
        }

        private void GenerateCSV (ScriptPlaylist script)
        {
            var builder = new StringBuilder("Path,Author,Text\n");
            foreach (var cmd in script.OfType<PrintText>())
            {
                var spot = AudioConfiguration.GetAutoVoiceClipPath(cmd.PlaybackSpot);
                var id = AudioConfiguration.GetAutoVoiceClipPath(cmd.Text);
                builder.Append($"{spot} #{id},{EscapeCSV(cmd.AuthorId?.ToString())},{EscapeCSV(ResolveText(script.ScriptName, cmd.Text))}\n");
            }
            File.WriteAllText($"{OutputPath}/{script.ScriptName}.csv", builder.ToString());

            string EscapeCSV (string text)
            {
                if (string.IsNullOrEmpty(text)) return "";
                return '"' + text.Replace("\"", "\"\"") + '"';
            }
        }

        private string ResolveText (string scriptName, LocalizableTextParameter param)
        {
            return LocalizableTextResolver.Resolve(param, ResolveId);

            string ResolveId (string id) => l10n.IsSourceLocaleSelected()
                ? scripts.GetScript(scriptName).TextMap.GetTextOrNull(id)
                : text.GetRecordValue(id, $"{ManagedTextConfiguration.ScriptMapCategory}/{scriptName}");
        }
    }
}
