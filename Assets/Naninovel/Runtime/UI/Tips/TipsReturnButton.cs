// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.UI
{
    public class TipsReturnButton : ScriptableButton
    {
        private ITipsUI tipsUI;

        protected override void Awake ()
        {
            base.Awake();

            tipsUI = GetComponentInParent<ITipsUI>();
        }

        protected override void OnButtonClick () => tipsUI.Hide();
    }
}
