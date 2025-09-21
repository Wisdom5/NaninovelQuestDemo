// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows ejecting localization resources for community localization feature.
    /// </summary>
    public class LocalizationEjector
    {
        private const string ejectArg = "-nani-eject";

        private readonly IResourceProviderManager providers;
        private readonly ILocalizationManager l10n;

        public LocalizationEjector (IResourceProviderManager providers, ILocalizationManager l10n)
        {
            this.providers = providers;
            this.l10n = l10n;
        }

        public static bool IsEjectionRequested ()
        {
            return Environment.GetCommandLineArgs().Any(a => a.StartsWithFast(ejectArg));
        }

        public void EjectScripts (string outDir)
        {
            if (ResolveEjectionLocale() == l10n.Configuration.SourceLocale)
                foreach (var script in Engine.GetService<IScriptManager>().Scripts)
                    EjectScript(script, outDir);
        }

        public async UniTask EjectTextAsync (string outDir)
        {
            var textLoader = Engine.GetConfiguration<ManagedTextConfiguration>().Loader
                .CreateLocalizableFor<TextAsset>(providers, l10n);
            textLoader.OverrideLocale = ResolveEjectionLocale();
            foreach (var resource in await textLoader.LoadAllAsync())
                EjectText(resource.Object.text, textLoader.GetLocalPath(resource), outDir);
        }

        private void EjectScript (Script script, string outDir)
        {
            var path = Path.Combine(outDir, $"{script.Name}.txt");
            var records = ManagedTextUtils.HashMap(script.TextMap.Map);
            var category = $"{ManagedTextConfiguration.ScriptMapCategory}/{script.Name}";
            var existing = File.Exists(path) ? ManagedTextUtils.Parse(File.ReadAllText(path), category).Records : null;
            var text = CreateLocalizer(category).Localize(records, existing);
            File.WriteAllText(path, text);
        }

        private void EjectText (string sourceText, string category, string outDir)
        {
            var path = Path.Combine(outDir, $"{category}.txt");
            var existingText = File.Exists(path) ? File.ReadAllText(path) : null;
            var text = CreateLocalizer(category).Localize(sourceText, existingText);
            File.WriteAllText(path, text);
        }

        private string ResolveEjectionLocale ()
        {
            var locale = Environment.GetCommandLineArgs()
                .First(a => a.StartsWithFast(ejectArg)).GetAfter(ejectArg + "-")?.TrimFull();
            return locale is null || !l10n.LocaleAvailable(locale)
                ? l10n.Configuration.SourceLocale : locale;
        }

        private ManagedTextLocalizer CreateLocalizer (string category)
        {
            var multiline = Engine.GetConfiguration<ManagedTextConfiguration>().IsMultilineCategory(category);
            return new ManagedTextLocalizer(new ManagedTextLocalizer.Options { Multiline = multiline });
        }
    }
}
