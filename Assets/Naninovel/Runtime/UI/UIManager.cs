// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.UI;
using TMPro;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IUIManager"/>
    [InitializeAtRuntime]
    public class UIManager : IUIManager, IStatefulService<SettingsStateMap>
    {
        [Serializable]
        public class Settings
        {
            public string FontName;
            public int FontSize = -1;
        }

        public event Action<string> OnFontNameChanged;
        public event Action<int> OnFontSizeChanged;

        public virtual UIConfiguration Configuration { get; }
        public virtual string FontName { get => fontName; set => SetFontName(value); }
        public virtual int FontSize { get => fontSize; set => SetFontSize(value); }

        private readonly List<ManagedUI> managedUIs = new List<ManagedUI>();
        private readonly Dictionary<Type, IManagedUI> cachedGetUIResults = new Dictionary<Type, IManagedUI>();
        private readonly Dictionary<string, TMP_FontAsset> fontNameToAsset = new Dictionary<string, TMP_FontAsset>();
        private readonly List<IManagedUI> modalUIs = new List<IManagedUI>();
        private readonly ICameraManager cameraManager;
        private readonly IInputManager inputManager;
        private readonly IResourceProviderManager providersManager;
        private readonly ILocalizationManager localizationManager;
        private LocalizableResourceLoader<GameObject> uiLoader;
        private LocalizableResourceLoader<TMP_FontAsset> fontLoader;
        private GameObject container;
        private GameObject modalContainer;
        private CanvasGroup containerGroup;
        private IInputSampler toggleUIInput;
        private string fontName;
        private int fontSize = -1;

        public UIManager (UIConfiguration config,
            IResourceProviderManager providersManager,
            ILocalizationManager localizationManager,
            ICameraManager cameraManager, IInputManager inputManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.localizationManager = localizationManager;
            this.cameraManager = cameraManager;
            this.inputManager = inputManager;

            // Instantiating the UIs after the engine initialization so that UIs can use Engine API in Awake() and OnEnable() methods.
            Engine.AddPostInitializationTask(InstantiateUIsAsync);
        }

        public virtual async UniTask InitializeServiceAsync ()
        {
            uiLoader = Configuration.UILoader.CreateLocalizableFor<GameObject>(providersManager, localizationManager);
            fontLoader = Configuration.FontLoader.CreateLocalizableFor<TMP_FontAsset>(providersManager, localizationManager);

            container = Engine.CreateObject("UI");
            containerGroup = container.AddComponent<CanvasGroup>();
            modalContainer = Engine.CreateObject("ModalUI");

            toggleUIInput = inputManager.GetToggleUI();
            if (toggleUIInput != null)
                toggleUIInput.OnStart += ToggleUI;

            foreach (var (name, asset) in await InitializeFontsAsync())
                fontNameToAsset[name] = asset;
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            if (toggleUIInput != null)
                toggleUIInput.OnStart -= ToggleUI;

            foreach (var ui in managedUIs)
                ObjectUtils.DestroyOrImmediate(ui.GameObject);
            managedUIs.Clear();
            cachedGetUIResults.Clear();
            fontNameToAsset.Clear();

            ObjectUtils.DestroyOrImmediate(container);
            ObjectUtils.DestroyOrImmediate(modalContainer);

            uiLoader?.ReleaseAll(this);
            fontLoader?.ReleaseAll(this);

            localizationManager.RemoveChangeLocaleTask(ApplyFontAssociatedWithLocale);

            Engine.RemovePostInitializationTask(InstantiateUIsAsync);
        }

        public virtual void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                FontName = FontName,
                FontSize = FontSize
            };
            stateMap.SetState(settings);
        }

        public virtual UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings {
                FontName = Configuration.DefaultFont
            };
            FontName = settings.FontName;
            FontSize = settings.FontSize;

            return UniTask.CompletedTask;
        }

        public virtual async UniTask<IManagedUI> AddUIAsync (GameObject prefab, string name = default, string group = default)
        {
            var uiComponent = InstantiatePrefab(prefab, name, group);
            await uiComponent.InitializeAsync();
            return uiComponent;
        }

        public virtual IReadOnlyCollection<IManagedUI> GetManagedUIs ()
        {
            return managedUIs.Select(u => u.UIComponent).ToArray();
        }

        public bool HasUI<T> () where T : class, IManagedUI
        {
            var type = typeof(T);
            if (cachedGetUIResults.ContainsKey(type)) return true;
            foreach (var managedUI in managedUIs)
                if (type.IsAssignableFrom(managedUI.ComponentType))
                    return true;
            return false;
        }

        public bool HasUI (string name)
        {
            foreach (var managedUI in managedUIs)
                if (managedUI.Name == name)
                    return true;
            return false;
        }

        public virtual T GetUI<T> () where T : class, IManagedUI => GetUI(typeof(T)) as T;

        public virtual IManagedUI GetUI (Type type)
        {
            if (cachedGetUIResults.TryGetValue(type, out var cachedResult))
                return cachedResult;

            foreach (var managedUI in managedUIs)
                if (type.IsAssignableFrom(managedUI.ComponentType))
                {
                    var result = managedUI.UIComponent;
                    cachedGetUIResults[type] = result;
                    return managedUI.UIComponent;
                }

            return null;
        }

        public virtual IManagedUI GetUI (string name)
        {
            foreach (var managedUI in managedUIs)
                if (managedUI.Name == name)
                    return managedUI.UIComponent;
            return null;
        }

        public virtual bool RemoveUI (IManagedUI managedUI)
        {
            if (!TryGetManaged(managedUI, out var ui))
                return false;

            managedUIs.Remove(ui);
            foreach (var kv in cachedGetUIResults.ToList())
                if (kv.Value == managedUI)
                    cachedGetUIResults.Remove(kv.Key);

            ObjectUtils.DestroyOrImmediate(ui.GameObject);

            return true;
        }

        public virtual void SetUIVisibleWithToggle (bool visible, bool allowToggle = true)
        {
            cameraManager.RenderUI = visible;

            var clickThroughPanel = GetUI<IClickThroughPanel>();
            if (clickThroughPanel is null) return;

            if (visible) clickThroughPanel.Hide();
            else
            {
                if (allowToggle) clickThroughPanel.Show(true, ToggleUI, InputNames.Submit, InputNames.ToggleUI);
                else clickThroughPanel.Show(false, null);
            }
        }

        public virtual void AddModalUI (IManagedUI managedUI)
        {
            if (IsModal(managedUI) || !TryGetManaged(managedUI, out var ui)) return;
            foreach (var otherModal in modalUIs)
                if (TryGetManaged(otherModal, out var otherModalUI) && ShouldYieldModal(otherModal, managedUI))
                    otherModalUI.GameObject.transform.SetParent(otherModalUI.Parent, false);
            ui.GameObject.transform.SetParent(modalContainer.transform, false);
            modalUIs.Insert(0, managedUI);
            containerGroup.interactable = false;
        }

        public virtual void RemoveModalUI (IManagedUI managedUI)
        {
            if (!IsModal(managedUI) || !TryGetManaged(managedUI, out var ui)) return;
            ui.GameObject.transform.SetParent(ui.Parent, false);
            modalUIs.Remove(managedUI);
            if (modalUIs.Count == 0)
                containerGroup.interactable = true;
            else if (TryGetManaged(modalUIs[0], out var topModalUI))
                topModalUI.GameObject.transform.SetParent(modalContainer.transform, false);
        }

        public TMP_FontAsset GetFontAsset (string fontName)
        {
            return fontNameToAsset.TryGetValue(fontName, out var asset) ? asset :
                throw new Error($"Failed to get '{fontName}' font asset: unknown font.");
        }

        protected virtual bool IsModal (IManagedUI managedUI)
        {
            return modalUIs.Contains(managedUI);
        }

        protected virtual bool ShouldYieldModal (IManagedUI existingModal, IManagedUI newModal)
        {
            if (string.IsNullOrEmpty(existingModal.ModalGroup)) return true;
            if (existingModal.ModalGroup == "*") return false;
            return existingModal.ModalGroup != newModal.ModalGroup;
        }

        protected virtual IManagedUI InstantiatePrefab (GameObject prefab, string name = default, string group = default)
        {
            var layer = Configuration.OverrideObjectsLayer ? (int?)Configuration.ObjectsLayer : null;
            var parent = string.IsNullOrEmpty(group) ? container.transform : GetOrCreateGroup(group);
            var gameObject = Engine.Instantiate(prefab, prefab.name, layer, parent);

            if (!gameObject.TryGetComponent<IManagedUI>(out var uiComponent))
                throw new Error($"Failed to instantiate `{prefab.name}` UI prefab: the prefab doesn't contain a `{nameof(CustomUI)}` or `{nameof(IManagedUI)}` component on the root object.");

            if (!uiComponent.RenderCamera)
                uiComponent.RenderCamera = cameraManager.UICamera ? cameraManager.UICamera : cameraManager.Camera;

            if (!string.IsNullOrEmpty(FontName)) uiComponent.SetFont(GetFontAsset(FontName));
            if (FontSize >= 0) uiComponent.SetFontSize(FontSize);

            var managedUI = new ManagedUI(name ?? prefab.name, gameObject, uiComponent);
            managedUIs.Add(managedUI);

            return uiComponent;
        }

        protected virtual Transform GetOrCreateGroup (string group)
        {
            var existing = container.transform.Find(group);
            if (!existing)
            {
                existing = new GameObject(group).transform;
                existing.parent = container.transform;
            }
            return existing;
        }

        protected virtual void SetFontName (string fontName)
        {
            if (FontName == fontName) return;

            this.fontName = fontName;

            OnFontNameChanged?.Invoke(fontName);

            if (string.IsNullOrEmpty(fontName))
            {
                foreach (var ui in managedUIs)
                    ui.UIComponent.SetFont(null);
                return;
            }

            foreach (var ui in managedUIs)
                ui.UIComponent.SetFont(GetFontAsset(fontName));
        }

        protected virtual void SetFontSize (int size)
        {
            if (fontSize == size) return;

            fontSize = size;

            OnFontSizeChanged?.Invoke(size);

            foreach (var ui in managedUIs)
                ui.UIComponent.SetFontSize(size);
        }

        protected virtual void ToggleUI () => SetUIVisibleWithToggle(!cameraManager.RenderUI);

        protected virtual async UniTask InstantiateUIsAsync ()
        {
            var resources = await uiLoader.LoadAndHoldAllAsync(this);
            foreach (var resource in resources)
                InstantiatePrefab(resource, uiLoader.GetLocalPath(resource));
            var tasks = managedUIs.Select(u => u.UIComponent.InitializeAsync());
            await UniTask.WhenAll(tasks);

            await ApplyFontAssociatedWithLocale();
            localizationManager.AddChangeLocaleTask(NotifyLocaleChanged, 1);
            localizationManager.AddChangeLocaleTask(ApplyFontAssociatedWithLocale, 2);

            ShowVisibleOnAwake();
        }

        protected virtual void ShowVisibleOnAwake ()
        {
            if (Engine.GetConfiguration<ScriptsConfiguration>().ShowScriptNavigator)
                GetUI<IScriptNavigatorUI>()?.Show();
            foreach (var ui in managedUIs)
                if (ui.UIComponent is CustomUI custom && custom.VisibleOnAwake)
                    custom.Show();
        }

        protected virtual bool TryGetManaged (IManagedUI ui, out ManagedUI managedUI)
        {
            managedUI = default;
            foreach (var mui in managedUIs)
                if (mui.UIComponent == ui)
                {
                    managedUI = mui;
                    return true;
                }
            return false;
        }

        protected virtual UniTask ApplyFontAssociatedWithLocale ()
        {
            if (!Configuration.FontOptions.Any(o => !string.IsNullOrEmpty(o.ApplyOnLocale)))
                return UniTask.CompletedTask;

            foreach (var option in Configuration.FontOptions)
                if (option.ApplyOnLocale == localizationManager.SelectedLocale)
                {
                    SetFontName(option.FontName);
                    return UniTask.CompletedTask;
                }

            if (!string.IsNullOrEmpty(FontName)) SetFontName("");
            return UniTask.CompletedTask;
        }

        protected virtual UniTask NotifyLocaleChanged ()
        {
            var tasks = new List<UniTask>();
            foreach (var ui in managedUIs)
                if (ui.UIComponent is ILocalizableUI locUI)
                    tasks.Add(locUI.HandleLocalizationChangedAsync());
            return UniTask.WhenAll(tasks);
        }

        protected virtual async UniTask<IEnumerable<(string Name, TMP_FontAsset Asset)>> InitializeFontsAsync ()
        {
            return await UniTask.WhenAll(Configuration.FontOptions.Select(LoadFontAsset));

            async UniTask<(string Name, TMP_FontAsset Asset)> LoadFontAsset (UIConfiguration.FontOption option)
            {
                var resource = await fontLoader.LoadAndHoldAsync(option.FontResource, this);
                return resource.Valid ? (option.FontName, resource.Object) : throw new Error($"Failed to load '{option.FontResource}' font resource.");
            }
        }
    }
}
