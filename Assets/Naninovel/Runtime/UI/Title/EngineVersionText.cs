// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class EngineVersionText : MonoBehaviour
    {
        private void Start ()
        {
            var version = EngineVersion.LoadFromResources();
            GetComponent<TMP_Text>().text = $"Naninovel v{version.Version}{Environment.NewLine}Build {version.Build}";
        }
    }
}
