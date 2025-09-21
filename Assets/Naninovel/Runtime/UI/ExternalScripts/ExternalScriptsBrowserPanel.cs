// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel.UI
{
    public class ExternalScriptsBrowserPanel : NavigatorPanel, IExternalScriptsUI
    {
        protected override IReadOnlyCollection<Script> Scripts => ScriptManager.ExternalScripts;
    }
}
