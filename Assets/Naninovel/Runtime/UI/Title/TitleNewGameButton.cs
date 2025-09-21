// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class TitleNewGameButton : ScriptableButton
    {
        [Tooltip("Services to exclude from state reset when starting a new game.")]
        [SerializeField] private string[] excludeFromReset = Array.Empty<string>();

        private string startScriptName;
        private TitleMenu titleMenu;
        private IScriptPlayer scriptPlayer;
        private IStateManager stateManager;
        private IScriptManager scriptManager;

        protected override void Awake ()
        {
            base.Awake();

            scriptManager = Engine.GetService<IScriptManager>();
            startScriptName = scriptManager.Configuration.StartGameScript;
            if (string.IsNullOrEmpty(startScriptName))
                startScriptName = scriptManager.Scripts.FirstOrDefault()?.Name;
            titleMenu = GetComponentInParent<TitleMenu>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();
            stateManager = Engine.GetService<IStateManager>();
            Debug.Assert(titleMenu && scriptPlayer != null);
        }

        protected override void Start ()
        {
            base.Start();

            if (string.IsNullOrEmpty(startScriptName))
                UIComponent.interactable = false;
        }

        protected override async void OnButtonClick ()
        {
            if (string.IsNullOrEmpty(startScriptName))
            {
                Engine.Err("Can't start new game: specify start script name in scripts configuration.");
                return;
            }

            await PlayTitleNewGame();
            titleMenu.Hide();
            stateManager.ResetStateAsync(excludeFromReset,
                () => scriptPlayer.PreloadAndPlayAsync(startScriptName)).Forget();
        }

        protected virtual async UniTask PlayTitleNewGame ()
        {
            const string label = "OnNewGame";
            var scriptName = scriptManager.Configuration.TitleScript;
            if (!scriptManager.TryGetScript(scriptName, out var script) || !script.LabelExists(label)) return;
            scriptPlayer.ResetService();
            await scriptPlayer.PreloadAndPlayAsync(scriptName, label: label);
            await UniTask.WaitWhile(() => scriptPlayer.Playing);
        }
    }
}
