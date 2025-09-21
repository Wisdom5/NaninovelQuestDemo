// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Commands
{
    /// <summary>
    /// Opens specified URL (web address) with default web browser.
    /// </summary>
    /// <remarks>
    /// When outside of WebGL or in editor, Unity's `Application.OpenURL` method is used to handle the command;
    /// consult the [documentation](https://docs.unity3d.com/ScriptReference/Application.OpenURL.html) for behaviour details and limitations.
    /// Under WebGL native `window.open()` JS function is invoked: https://developer.mozilla.org/en-US/docs/Web/API/Window/open.
    /// </remarks>
    public class OpenURL : Command
    {
        /// <summary>
        /// URL to open.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter URL;
        /// <summary>
        /// Browsing context: _self (current tab), _blank (new tab), _parent, _top.
        /// </summary>
        [ParameterDefaultValue("_self")]
        public StringParameter Target = "_self";

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            WebUtils.OpenURL(URL, Target);
            return UniTask.CompletedTask;
        }
    }
}
