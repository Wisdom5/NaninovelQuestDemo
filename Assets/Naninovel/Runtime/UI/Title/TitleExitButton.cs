// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class TitleExitButton : ScriptableButton
    {
        private IScriptPlayer scriptPlayer;
        private IScriptManager scriptManager;
        private IStateManager stateManager;

        protected override void Awake ()
        {
            base.Awake();

            scriptManager = Engine.GetService<IScriptManager>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();
            stateManager = Engine.GetService<IStateManager>();
        }

        protected override async void OnButtonClick ()
        {
            await PlayTitleExit();
            await stateManager.SaveGlobalAsync();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                WebUtils.OpenURL("about:blank");
            else Application.Quit();
        }

        protected virtual async UniTask PlayTitleExit ()
        {
            const string label = "OnExit";
            var scriptName = scriptManager.Configuration.TitleScript;
            if (!scriptManager.TryGetScript(scriptName, out var script) || !script.LabelExists(label)) return;
            scriptPlayer.ResetService();
            await scriptPlayer.PreloadAndPlayAsync(scriptName, label: label);
            await UniTask.WaitWhile(() => scriptPlayer.Playing);
        }
    }
}
