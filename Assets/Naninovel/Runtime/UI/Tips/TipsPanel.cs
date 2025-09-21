// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TipsPanel : CustomUI, ITipsUI
    {
        public const string DefaultUnlockableIdPrefix = "Tips";

        public virtual int TipsCount { get; private set; }

        protected virtual string UnlockableIdPrefix => unlockableIdPrefix;
        protected virtual string ManagedTextCategory => managedTextCategory;
        protected virtual RectTransform ItemsContainer => itemsContainer;
        protected virtual TipsListItem ItemPrefab => itemPrefab;
        protected virtual string SelectedItemId { get; set; }

        private const string separatorLiteral = "|";
        private const string selectedPrefix = "TIP_SELECTED_";

        [Header("Tips Setup")]
        [Tooltip("All the unlockable item IDs with the specified prefix will be considered Tips items.")]
        [SerializeField] private string unlockableIdPrefix = DefaultUnlockableIdPrefix;
        [Tooltip("The name of the managed text document (category) where all the tips data is stored.")]
        [SerializeField] private string managedTextCategory = ManagedTextConfiguration.TipCategory;

        [Header("UI Setup")]
        [SerializeField] private ScrollRect itemsScrollRect;
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private TipsListItem itemPrefab;
        [SerializeField] private StringUnityEvent onTitleChanged;
        [SerializeField] private StringUnityEvent onNumberChanged;
        [SerializeField] private StringUnityEvent onCategoryChanged;
        [SerializeField] private StringUnityEvent onDescriptionChanged;

        private readonly List<TipsListItem> listItems = new List<TipsListItem>();
        private IUnlockableManager unlockableManager;
        private IInputManager inputManager;
        private ITextManager textManager;

        public override UniTask InitializeAsync ()
        {
            FillListItems();
            Engine.GetService<ILocalizationManager>().OnLocaleChanged += HandleLocaleChanged;
            return UniTask.CompletedTask;
        }

        public virtual void SelectTipRecord (string tipId)
        {
            var unlockableId = $"{UnlockableIdPrefix}/{tipId}";
            var item = listItems.Find(i => i.UnlockableId == unlockableId);
            if (item is null) throw new Error($"Failed to select `{tipId}` tip record: item with the ID is not found.");
            itemsScrollRect.ScrollTo(item.GetComponent<RectTransform>());
            SelectItem(item);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(itemsScrollRect, ItemsContainer, ItemPrefab);

            unlockableManager = Engine.GetService<IUnlockableManager>();
            inputManager = Engine.GetService<IInputManager>();
            textManager = Engine.GetService<ITextManager>();

            ClearSelection();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            unlockableManager.OnItemUpdated += HandleUnlockableItemUpdated;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (unlockableManager != null)
                unlockableManager.OnItemUpdated -= HandleUnlockableItemUpdated;
        }

        protected virtual void FillListItems ()
        {
            var records = textManager.GetAllRecords(ManagedTextCategory);
            foreach (var record in records)
            {
                var unlockableId = $"{UnlockableIdPrefix}/{record.Key}";
                var value = string.IsNullOrEmpty(record.Value) ? textManager.GetRecordValueWithFallback(record.Key, ManagedTextCategory) : record.Value;
                var title = value.GetBefore(separatorLiteral) ?? value;
                var selectedOnce = WasItemSelectedOnce(unlockableId);
                var item = TipsListItem.Instantiate(ItemPrefab, unlockableId, title, selectedOnce, SelectItem);
                item.transform.SetParent(ItemsContainer, false);
                listItems.Add(item);
            }

            foreach (var item in listItems)
                item.SetUnlocked(unlockableManager.ItemUnlocked(item.UnlockableId));

            TipsCount = listItems.Count;
        }

        protected virtual void ClearListItems ()
        {
            foreach (var item in listItems.ToArray())
                ObjectUtils.DestroyOrImmediate(item.gameObject);
            listItems.Clear();
            ItemsContainer.DetachChildren();
            TipsCount = 0;
        }

        protected virtual void ClearSelection ()
        {
            SetTitle(string.Empty);
            SetNumber(string.Empty);
            SetCategory(string.Empty);
            SetDescription(string.Empty);
            foreach (var item in listItems)
                item.SetSelected(false);
        }

        protected virtual void SelectItem (TipsListItem item)
        {
            if (!unlockableManager.ItemUnlocked(item.UnlockableId)) return;

            SelectedItemId = item.UnlockableId;
            SetItemSelectedOnce(item.UnlockableId);
            foreach (var listItem in listItems)
                listItem.SetSelected(listItem.UnlockableId.EqualsFast(item.UnlockableId));
            var recordValue = textManager.GetRecordValueWithFallback(item.UnlockableId.GetAfterFirst($"{UnlockableIdPrefix}/"), ManagedTextCategory);
            SetTitle(recordValue.GetBefore(separatorLiteral)?.Trim() ?? recordValue);
            SetNumber(item.Number.ToString());
            SetCategory(recordValue.GetBetween(separatorLiteral)?.Trim() ?? string.Empty);
            SetDescription(recordValue.GetAfter(separatorLiteral)?.Replace("\\n", "\n").Trim() ?? string.Empty);
            EventUtils.Select(item.gameObject);
        }

        protected virtual void HandleUnlockableItemUpdated (UnlockableItemUpdatedArgs args)
        {
            if (!args.Id.StartsWithFast(UnlockableIdPrefix)) return;

            var unlockedItem = listItems.Find(i => i.UnlockableId.EqualsFast(args.Id));
            if (unlockedItem) unlockedItem.SetUnlocked(args.Unlocked);
        }

        protected virtual void SetTitle (string value)
        {
            onTitleChanged?.Invoke(value);
        }

        protected virtual void SetNumber (string value)
        {
            onNumberChanged?.Invoke(value);
        }

        protected virtual void SetCategory (string value)
        {
            onCategoryChanged?.Invoke(value);
        }

        protected virtual void SetDescription (string value)
        {
            onDescriptionChanged?.Invoke(value);
        }

        protected virtual bool WasItemSelectedOnce (string unlockableId)
        {
            return unlockableManager.ItemUnlocked(selectedPrefix + unlockableId);
        }

        protected virtual void SetItemSelectedOnce (string unlockableId)
        {
            unlockableManager.SetItemUnlocked(selectedPrefix + unlockableId, true);
        }

        protected virtual void HandleLocaleChanged (string locale)
        {
            ClearSelection();
            ClearListItems();
            FillListItems();
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float? duration = null, AsyncToken asyncToken = default)
        {
            if (!visible)
                using (new InteractionBlocker())
                    await Engine.GetService<IStateManager>().SaveGlobalAsync();
            await base.ChangeVisibilityAsync(visible, duration, asyncToken);
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (inputManager.TryGetSampler(InputNames.Cancel, out var cancel))
                if (visible) cancel.OnEnd += Hide;
                else cancel.OnEnd -= Hide;
            if (inputManager.TryGetSampler(InputNames.NavigateY, out var nav))
                if (visible) nav.OnChange += HandleNavigationInput;
                else nav.OnChange -= HandleNavigationInput;

            if (visible && (FindSelectedItem() ?? FindFirstUnlockedItem()) is TipsListItem item)
                SelectItem(item);
        }

        protected virtual TipsListItem FindSelectedItem ()
        {
            return listItems.FirstOrDefault(i => i.UnlockableId == SelectedItemId);
        }

        protected virtual TipsListItem FindFirstUnlockedItem ()
        {
            return listItems.FirstOrDefault(i => unlockableManager.ItemUnlocked(i.UnlockableId));
        }

        protected virtual TipsListItem FindLastUnlockedItem ()
        {
            return listItems.LastOrDefault(i => unlockableManager.ItemUnlocked(i.UnlockableId));
        }

        protected virtual void HandleNavigationInput (float value)
        {
            if (!Visible) return;
            if (value <= -1f) SelectPreviousUnlockedItem();
            if (value >= 1f) SelectNextUnlockedItem();
        }

        protected virtual void SelectPreviousUnlockedItem ()
        {
            for (var i = listItems.IndexOf(FindSelectedItem()) - 1; i >= 0; i--)
                if (unlockableManager.ItemUnlocked(listItems[i].UnlockableId))
                {
                    SelectItem(listItems[i]);
                    return;
                }
            if (FindLastUnlockedItem() is TipsListItem last) SelectItem(last);
        }

        protected virtual void SelectNextUnlockedItem ()
        {
            for (var i = listItems.IndexOf(FindSelectedItem()) + 1; i < listItems.Count; i++)
                if (unlockableManager.ItemUnlocked(listItems[i].UnlockableId))
                {
                    SelectItem(listItems[i]);
                    return;
                }
            if (FindFirstUnlockedItem() is TipsListItem first) SelectItem(first);
        }
    }
}
