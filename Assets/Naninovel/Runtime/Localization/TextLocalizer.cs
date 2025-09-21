// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Text;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <inheritdoc cref="ITextLocalizer"/>
    [InitializeAtRuntime]
    public class TextLocalizer : ITextLocalizer
    {
        private readonly Dictionary<string, string> scriptNameToCategory = new Dictionary<string, string>();
        private readonly StringBuilder builder = new StringBuilder();
        private readonly ITextManager textManager;
        private readonly IScriptManager scriptManager;
        private readonly ILocalizationManager l10nManager;
        private readonly ICommunityLocalization communityL10n;

        public TextLocalizer (ITextManager textManager, IScriptManager scriptManager,
            ILocalizationManager l10nManager, ICommunityLocalization communityL10n)
        {
            this.textManager = textManager;
            this.scriptManager = scriptManager;
            this.l10nManager = l10nManager;
            this.communityL10n = communityL10n;
        }

        public virtual UniTask InitializeServiceAsync () => UniTask.CompletedTask;

        public virtual void ResetService () { }

        public virtual void DestroyService () { }

        public virtual string Resolve (LocalizableText text)
        {
            if (text.Parts.Count == 1 && !text.Parts[0].PlainText)
                return Resolve(text.Parts[0].Id, text.Parts[0].Script);
            builder.Clear();
            foreach (var part in text.Parts)
                builder.Append(part.PlainText ? part.Text : Resolve(part.Id, part.Script));
            return builder.ToString();
        }

        protected virtual string ScriptNameToCategory (string scriptName)
        {
            return scriptNameToCategory.TryGetValue(scriptName, out var category) ? category :
                scriptNameToCategory[scriptName] = $"{ManagedTextConfiguration.ScriptMapCategory}/{scriptName}";
        }

        protected virtual string Resolve (string id, string scriptName)
        {
            if (!communityL10n.Active && l10nManager.IsSourceLocaleSelected())
                return ResolveFromScript(id, scriptName);
            return ResolveFromDocument(id, scriptName);
        }

        protected virtual string ResolveFromScript (string id, string scriptName)
        {
            var value = scriptManager.GetScript(scriptName)?.TextMap?.GetTextOrNull(id);
            if (!string.IsNullOrEmpty(value)) return value;
            Engine.Warn($"Failed to resolve localized text for `{scriptName}`: script or text mapping is not available.");
            return $"{Identifiers.TextIdOpen}{scriptName}/{id}{Identifiers.TextIdClose}";
        }

        protected virtual string ResolveFromDocument (string id, string scriptName)
        {
            var category = ScriptNameToCategory(scriptName);
            var value = textManager.GetRecordValue(id, category);
            if (!string.IsNullOrEmpty(value)) return value;
            Engine.Warn($"Failed to resolve `{scriptName}/{id}` localized text; will use source locale.");
            return ResolveFromScript(id, scriptName);
        }
    }
}
