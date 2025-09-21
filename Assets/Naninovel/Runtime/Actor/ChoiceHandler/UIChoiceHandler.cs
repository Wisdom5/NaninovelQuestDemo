// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IChoiceHandlerActor"/> implementation using <see cref="UI.ChoiceHandlerPanel"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(ChoiceHandlerPanel), false)]
    public class UIChoiceHandler : MonoBehaviourActor<ChoiceHandlerMetadata>, IChoiceHandlerActor
    {
        public override GameObject GameObject => HandlerPanel.gameObject;
        public override string Appearance { get; set; }
        public override bool Visible { get => HandlerPanel.Visible; set => HandlerPanel.Visible = value; }
        public virtual List<ChoiceState> Choices { get; } = new List<ChoiceState>();

        protected virtual ChoiceHandlerPanel HandlerPanel { get; private set; }

        private readonly IStateManager stateManager;
        private readonly IUIManager uiManager;

        public UIChoiceHandler (string id, ChoiceHandlerMetadata metadata)
            : base(id, metadata)
        {
            stateManager = Engine.GetService<IStateManager>();
            uiManager = Engine.GetService<IUIManager>();
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            var prefab = await LoadUIPrefabAsync();
            HandlerPanel = await uiManager.AddUIAsync(prefab, group: BuildActorCategory()) as ChoiceHandlerPanel;
            if (!HandlerPanel) throw new Error($"Failed to initialize `{Id}` choice handler actor: choice panel UI instantiation failed.");
            HandlerPanel.OnChoice += HandleChoice;
            Visible = false;
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            if (HandlerPanel)
                await HandlerPanel.ChangeVisibilityAsync(visible, duration);
        }

        public virtual void AddChoice (ChoiceState choice)
        {
            Choices.Add(choice);
            HandlerPanel.AddChoiceButton(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            Choices.RemoveAll(c => c.Id == id);
            HandlerPanel.RemoveChoiceButton(id);
        }

        public virtual void HandleChoice (string id)
        {
            if (!Choices.Any(c => c.Id == id))
                throw new Error($"Failed to handle choice with ID '{id}': choice not found.");
            HandleChoice(Choices.First(c => c.Id == id));
        }

        public virtual ChoiceState GetChoice (string id) => Choices.FirstOrDefault(c => c.Id == id);

        public override void Dispose ()
        {
            base.Dispose();

            if (HandlerPanel != null)
            {
                uiManager.RemoveUI(HandlerPanel);
                ObjectUtils.DestroyOrImmediate(HandlerPanel.gameObject);
                HandlerPanel = null;
            }
        }

        protected virtual async UniTask<GameObject> LoadUIPrefabAsync ()
        {
            var providerManager = Engine.GetService<IResourceProviderManager>();
            var localizationManager = Engine.GetService<ILocalizationManager>();
            var resource = await ActorMetadata.Loader.CreateLocalizableFor<GameObject>(providerManager, localizationManager).LoadAsync(Id);
            if (!resource.Valid) throw new Error($"Failed to load `{Id}` choice handler resource object. Make sure the handler is correctly configured.");
            return resource;
        }

        protected override GameObject CreateHostObject () => null;

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        protected virtual async void HandleChoice (ChoiceState state)
        {
            if (!Choices.Exists(c => c.Id.EqualsFast(state.Id))) return;

            stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            Choices.Clear();

            var player = Engine.GetService<IScriptPlayer>();
            var scriptText = state.OnSelectScript;

            if (HandlerPanel)
            {
                HandlerPanel.RemoveAllChoiceButtonsDelayed(); // Delayed to allow custom onClick logic.
                HandlerPanel.Hide();
                if (ActorMetadata.WaitHideOnChoice)
                    scriptText = $"@wait {HandlerPanel.FadeTime}\n" + scriptText;
            }

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(GetDestroyCancellationToken()))
            {
                stateManager.OnRollbackStarted += cts.Cancel;
                try { await player.PlayTransient($"`{Id}` on choice script", scriptText, cts.Token); }
                catch (OperationCanceledException) { return; }
                finally
                {
                    if (stateManager != null)
                        stateManager.OnRollbackStarted -= cts.Cancel;
                    cts.Dispose();
                }
            }

            if (state.AutoPlay && !player.Playing)
            {
                var nextIndex = player.PlayedIndex + 1;
                player.Play(nextIndex);
            }
        }
    }
}
