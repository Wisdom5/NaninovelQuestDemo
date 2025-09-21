// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICommunityLocalization"/>
    [InitializeAtRuntime]
    public class CommunityLocalization : ICommunityLocalization
    {
        public virtual bool Active { get; private set; }
        public virtual string Author { get; private set; } = "";

        protected virtual string Root => Path.Combine(Application.persistentDataPath, "Localization");
        protected virtual string AuthorFilePath => Path.Combine(Root, "Author.txt");
        protected virtual string ScriptFolder => Path.Combine(Root, "Scripts");
        protected virtual string TextFolder => Path.Combine(Root, "Text");

        public virtual async UniTask InitializeServiceAsync ()
        {
            if (!IsPlatformSupported()) return;

            try
            {
                Active = File.Exists(AuthorFilePath);
                if (Active)
                {
                    Author = await ResolveAuthorAsync();
                    Engine.Log($"Community localization by `{Author}` is active.");
                }
            }
            catch { Active = false; }

            if (IsEjectionRequested())
                Engine.AddPostInitializationTask(EjectAsync);
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            Engine.RemovePostInitializationTask(EjectAsync);
        }

        public virtual async UniTask<IReadOnlyList<(string Text, string Category)>> LoadLocalizedDocumentsAsync ()
        {
            if (!Directory.Exists(Root)) return Array.Empty<(string Text, string Category)>();
            var documents = new List<(string Text, string Category)>();
            if (Directory.Exists(TextFolder))
                foreach (string path in Directory.EnumerateFiles(TextFolder, "*.txt", SearchOption.TopDirectoryOnly))
                    documents.Add((await IOUtils.ReadTextFileAsync(path), Path.GetFileNameWithoutExtension(path)));
            if (Directory.Exists(ScriptFolder))
                foreach (string path in Directory.EnumerateFiles(ScriptFolder, "*.txt", SearchOption.AllDirectories))
                    documents.Add((await IOUtils.ReadTextFileAsync(path), PathUtils.FormatPath(path).Substring(Root.Length + 1).GetBeforeLast(".txt")));
            return documents;
        }

        protected virtual bool IsPlatformSupported ()
        {
            return Application.isEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.LinuxPlayer ||
                   Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }

        protected virtual bool IsEjectionRequested ()
        {
            return LocalizationEjector.IsEjectionRequested();
        }

        protected virtual async UniTask EjectAsync ()
        {
            Directory.CreateDirectory(ScriptFolder);
            Directory.CreateDirectory(TextFolder);
            if (!File.Exists(AuthorFilePath))
                File.WriteAllText(AuthorFilePath, "Unknown");
            var ejector = new LocalizationEjector(
                Engine.GetService<IResourceProviderManager>(),
                Engine.GetService<ILocalizationManager>());
            ejector.EjectScripts(ScriptFolder);
            await ejector.EjectTextAsync(TextFolder);
            Engine.Log($"Ejected community localization resources to `{Root}`.");
        }

        protected virtual string BuildScriptPath (string scriptName)
        {
            return Path.Combine(ScriptFolder, $"{scriptName}.nani");
        }

        protected virtual async UniTask<string> ResolveAuthorAsync ()
        {
            var author = (await IOUtils.ReadTextFileAsync(AuthorFilePath)).TrimFull();
            return string.IsNullOrWhiteSpace(author) ? "Unknown" : author;
        }
    }
}
