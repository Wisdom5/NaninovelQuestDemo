// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;
using System.Threading;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class GameSettingsPreviewPrinter : ScriptableUIBehaviour
    {
        protected virtual IRevealableText RevealableText { get; private set; }

        [Tooltip("A component implementing `" + nameof(IRevealableText) + "` interface for displaying the preview text.")]
        [SerializeField] private Graphic revealableText;

        private TextRevealer revealer;
        private CancellationTokenSource revealCTS;
        private ITextPrinterManager printerManager;

        public virtual void StartPrinting ()
        {
            revealCTS?.Cancel();

            RevealableText.RevealProgress = 0;
            if (RevealableText is Graphic graphic)
                graphic.Rebuild(CanvasUpdate.PreRender); // Otherwise it's not displaying anything.

            var revealDelay = Mathf.Lerp(printerManager.Configuration.MaxRevealDelay, 0, printerManager.BaseRevealSpeed);
            if (revealDelay == 0)
                RevealableText.RevealProgress = 1;
            else
            {
                revealCTS = new CancellationTokenSource();
                RevealTextOverTimeAsync(revealDelay, revealCTS.Token).Forget();
            }
        }

        protected override void Awake ()
        {
            base.Awake();

            RevealableText = revealableText as IRevealableText;
            if (RevealableText is null)
                throw new Error($"Field `{nameof(revealableText)}` on `{nameof(GameSettingsPreviewPrinter)}` component is either not assigned or doesn't implement `{nameof(IRevealableText)}` interface.");
            revealer = new TextRevealer(RevealableText);
            printerManager = Engine.GetService<ITextPrinterManager>();
            GetComponentInParent<IManagedUI>().OnVisibilityChanged += HandleRootVisibilityChanged;
        }

        protected virtual async UniTask RevealTextOverTimeAsync (float revealDelay, CancellationToken token)
        {
            await revealer.RevealAsync(revealDelay, token);
            if (token.IsCancellationRequested) return;

            var autoPlayDelay = Mathf.Lerp(0, printerManager.Configuration.MaxAutoWaitDelay,
                printerManager.BaseAutoDelay) * RevealableText.Text.Count(char.IsLetterOrDigit);
            var waitUntilTime = Engine.Time.UnscaledTime + autoPlayDelay;
            while (Engine.Time.UnscaledTime < waitUntilTime)
                await AsyncUtils.WaitEndOfFrameAsync(token);

            if (!token.IsCancellationRequested)
                StartPrinting();
        }

        protected virtual void HandleRootVisibilityChanged (bool visible)
        {
            if (visible) StartPrinting();
            else revealCTS?.Cancel();
        }
    }
}
