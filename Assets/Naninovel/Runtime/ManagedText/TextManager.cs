// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.ManagedText;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ITextManager"/>
    [InitializeAtRuntime]
    public class TextManager : ITextManager
    {
        public virtual ManagedTextConfiguration Configuration { get; }

        private readonly IResourceProviderManager providersManager;
        private readonly ILocalizationManager l10nManager;
        private readonly ICommunityLocalization communityL10n;
        private readonly ILookup<string, Type> nameToEngineTypes = Engine.Types.ToLookup(t => t.FullName);
        private readonly Dictionary<string, ManagedTextDocument> categoryToDoc = new Dictionary<string, ManagedTextDocument>();
        private LocalizableResourceLoader<TextAsset> documentLoader;

        public TextManager (ManagedTextConfiguration config, IResourceProviderManager providersManager,
            ILocalizationManager l10nManager, ICommunityLocalization communityL10n)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.l10nManager = l10nManager;
            this.communityL10n = communityL10n;
        }

        public virtual async UniTask InitializeServiceAsync ()
        {
            l10nManager.AddChangeLocaleTask(HandleLocaleChanged);
            documentLoader = Configuration.Loader.CreateLocalizableFor<TextAsset>(providersManager, l10nManager);
            documentLoader.OnLoaded += HandleDocumentLoaded;
            documentLoader.OnUnloaded += HandleDocumentUnloaded;
            await documentLoader.LoadAndHoldAllAsync(this);
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            documentLoader.OnLoaded -= HandleDocumentLoaded;
            documentLoader.OnUnloaded -= HandleDocumentUnloaded;
            documentLoader.ReleaseAll(this);
            l10nManager.RemoveChangeLocaleTask(HandleLocaleChanged);
        }

        public IReadOnlyCollection<string> GetAllCategories ()
        {
            return categoryToDoc.Keys;
        }

        public virtual IReadOnlyCollection<ManagedTextRecord> GetAllRecords (string category)
        {
            return categoryToDoc.TryGetValue(category, out var doc) ? doc.Records : Array.Empty<ManagedTextRecord>();
        }

        public bool TryGetRecord (string key, string category, out ManagedTextRecord record)
        {
            record = default;
            return categoryToDoc.TryGetValue(category, out var doc) && doc.TryGet(key, out record);
        }

        protected virtual bool IsScriptCategory (string category)
        {
            const string prefix = ManagedTextConfiguration.ScriptMapCategory + "/";
            return category.StartsWithFast(prefix);
        }

        protected virtual void AssignManagedFields ()
        {
            foreach (var category in categoryToDoc.Keys)
                if (!IsScriptCategory(category))
                    foreach (var record in GetAllRecords(category))
                        if (TryGetManagedFieldType(record.Key, out var fieldType))
                            ApplyManagedFieldRecord(record, fieldType);
        }

        protected virtual ManagedTextDocument ParseDocument (Resource<TextAsset> resource)
        {
            return ParseDocument(resource.Object.text, GetCategory(resource));
        }

        protected virtual ManagedTextDocument ParseDocument (string documentText, string category)
        {
            return ManagedTextUtils.Parse(documentText, category);
        }

        protected virtual string GetCategory (Resource<TextAsset> resource)
        {
            return documentLoader.GetLocalPath(resource);
        }

        protected virtual bool TryGetManagedFieldType (string recordKey, out Type fieldType)
        {
            var typeName = recordKey.GetBeforeLast(".") ?? recordKey;
            fieldType = nameToEngineTypes[typeName].FirstOrDefault();
            return fieldType != null;
        }

        protected virtual void ApplyManagedFieldRecord (ManagedTextRecord record, Type fieldType)
        {
            var fieldName = record.Key.GetAfter(".") ?? record.Key;
            var fieldInfo = fieldType.GetField(fieldName, ManagedTextUtils.ManagedFieldBindings);
            if (fieldInfo is null) Engine.Warn($"Failed to apply managed text record value to '{fieldType.FullName}.{fieldName}' field.");
            else fieldInfo.SetValue(null, GetValueWithFallback(record));
        }

        protected virtual async UniTask LoadCommunityRecords ()
        {
            foreach (var (text, category) in await communityL10n.LoadLocalizedDocumentsAsync())
                categoryToDoc[category] = ParseDocument(text, category);
        }

        protected virtual void HandleDocumentLoaded (Resource<TextAsset> document)
        {
            categoryToDoc[GetCategory(document)] = ParseDocument(document);
        }

        protected virtual void HandleDocumentUnloaded (Resource<TextAsset> document)
        {
            categoryToDoc.Remove(GetCategory(document));
        }

        protected virtual async UniTask HandleLocaleChanged ()
        {
            if (communityL10n.Active) await LoadCommunityRecords();
            else await documentLoader.LoadAndHoldAllAsync(this);
            AssignManagedFields();
        }

        protected virtual string GetValueWithFallback (ManagedTextRecord record)
        {
            if (!string.IsNullOrEmpty(record.Value)) return record.Value;
            if (!string.IsNullOrEmpty(record.Comment)) return record.Comment;
            return record.Key;
        }
    }
}
