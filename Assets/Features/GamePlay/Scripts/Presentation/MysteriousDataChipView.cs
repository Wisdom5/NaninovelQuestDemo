using System;
using System.Threading;
using DG.Tweening;
using Features.GamePlay.Scripts.Data;
using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.UI;
using Engine = Naninovel.Engine;

public class MysteriousDataChipView : CustomUI
{
    private const float ANIMATION_DURATION = 0.15f;
    private const float SCALE_MULTIPLIER = 1.3f;

    [SerializeField]
    private Button _chipButton;

    private MysteriousDataChipService _chipService;

    protected override void Start()
    {
        _chipService = Engine.GetService<MysteriousDataChipService>();
    }

    protected override void OnEnable()
    {
        _chipButton.onClick.AddListener(OnChipButtonClicked);
    }

    protected override void OnDisable()
    {
        _chipButton.onClick.RemoveListener(OnChipButtonClicked);
    }

    private void OnChipButtonClicked()
    {
        HandleClickAsync().Forget();
    }

    private async UniTask HandleClickAsync()
    {
        try
        {
            await PlayPickupAnimation(_chipButton.transform);

            _chipService.PickUpChip();

            var uiManager = Engine.GetService<IUIManager>();
            var ui = uiManager.GetUI(GamePlayUIKeys.MYSTERIOUS_CHIP_UI_KEY);

            if (ui != null)
            {
                await ui.ChangeVisibilityAsync(false, asyncToken: CancellationToken.None);
                ResetSize();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MysteriousDataChipView] Error handling chip click: {exception}");
        }
    }

    private void ResetSize()
    {
        if (_chipButton != null)
        {
            _chipButton.transform.localScale = Vector3.one;
        }
    }

    private async UniTask PlayPickupAnimation(Transform target)
    {
        if (target == null)
        {
            return;
        }

        var originalScale = target.localScale;

        var sequence = DOTween.Sequence()
            .Append(target.DOScale(originalScale * SCALE_MULTIPLIER, ANIMATION_DURATION))
            .Append(target.DOScale(originalScale * 0f, ANIMATION_DURATION));

        sequence.SetLink(target.gameObject);

        await sequence.AsyncWaitForCompletion();
    }
}
