// Copyright 2023 ReWaffle LLC. All rights reserved.

using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// When text overflow is <see cref="TextOverflowModes.Page"/> keeps current
    /// page in sync with the reveal progress.
    /// </summary>
    public class RevealPaginator : TextRevealEffect
    {
        private void OnEnable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
            Info.OnChange += HandleRevealChanged;
        }

        private void OnDisable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            if (Text) Info.OnChange -= HandleRevealChanged;
        }

        private void HandleRevealChanged ()
        {
            Text.pageToDisplay = GetPageNumber(Info.LastRevealedCharIndex);
        }

        private int GetPageNumber (int charIndex)
        {
            for (int i = 0; i < Text.textInfo.pageCount; i++)
                if (IsInsidePage(Text.textInfo.pageInfo[i], charIndex))
                    return i + 1;
            return 1;
        }

        private bool IsInsidePage (TMP_PageInfo page, int charIndex)
        {
            return charIndex >= page.firstCharacterIndex && charIndex <= page.lastCharacterIndex;
        }

        private void HandleTextChanged (Object obj)
        {
            if (obj != Text) return;
            var oldPage = Text.pageToDisplay;
            HandleRevealChanged();
            // TMP fails to update the mesh when page is changed while handling text change event.
            if (oldPage == Text.pageToDisplay) return;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            Text.ForceMeshUpdate();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        }
    }
}
