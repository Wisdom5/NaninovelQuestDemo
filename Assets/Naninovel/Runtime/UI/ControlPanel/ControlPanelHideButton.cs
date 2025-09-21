// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class ControlPanelHideButton : ScriptableLabeledButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick () => uiManager.SetUIVisibleWithToggle(false);
    } 
}
