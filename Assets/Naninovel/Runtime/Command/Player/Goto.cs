// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Navigates naninovel script playback to the provided path.
    /// </summary>
    public class Goto : Command, Command.IForceWait, Command.IPreloadable
    {
        /// <summary>
        /// When applied to an <see cref="IEngineService"/> implementation, the service won't be reset
        /// while executing the goto command and <see cref="StateConfiguration.ResetOnGoto"/> is enabled.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class DontResetAttribute : Attribute { }

        /// <summary>
        /// When assigned to <see cref="ResetState"/>, forces reset of all the services, 
        /// except the ones with <see cref="DontResetAttribute"/>.
        /// </summary>
        public const string ResetAllFlag = "*";
        /// <summary>
        /// When assigned to <see cref="ResetState"/>, forces no reset.
        /// </summary>
        public const string NoResetFlag = "-";

        /// <summary>
        /// Path to navigate into in the following format: `ScriptName.LabelName`.
        /// When label name is omitted, will play provided script from the start.
        /// When script name is omitted, will attempt to find a label in the currently played script.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, EndpointContext]
        public NamedStringParameter Path;
        /// <summary>
        /// When specified, will control whether to reset the engine services state before loading a script (in case the path is leading to another script):<br/>
        /// - Specify `*` to reset all the services, except the ones with `Goto.DontReset` attribute.<br/>
        /// - Specify service type names (separated by comma) to exclude from reset; all the other services will be reset, including the ones with `Goto.DontReset` attribute.<br/>
        /// - Specify `-` to force no reset (even if it's enabled by default in the configuration).<br/><br/>
        /// Notice, that while some services have `Goto.DontReset` attribute applied and are not reset by default, they should still be specified when excluding specific services from reset.
        /// </summary>
        [ParameterAlias("reset")]
        public StringListParameter ResetState;

        protected IScriptPlayer Player => Engine.GetService<IScriptPlayer>();

        private static Type[] dontResetTypes;

        public virtual UniTask PreloadResourcesAsync ()
        {
            EnsureDontResetTypesLoaded();
            return UniTask.CompletedTask;
        }

        public virtual void ReleasePreloadedResources () { }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            EnsureDontResetTypesLoaded();

            var scriptName = Path.Name;
            var label = Path.NamedValue;

            if (string.IsNullOrWhiteSpace(scriptName) && !Player.PlayedScript)
            {
                Err("Failed to execute `@goto` command: script name is not specified and no script is currently played.");
                return;
            }

            if (ShouldNavigatePlayedScript(scriptName)) NavigatePlayedScript(label);
            else if (Player.Configuration.LoadOnGoto)
                using (await LoadingScreen.ShowAsync(asyncToken))
                    await PreloadAndNavigateScriptAsync(scriptName, label);
            else await PreloadAndNavigateScriptAsync(scriptName, label);
        }

        protected virtual bool ShouldNavigatePlayedScript (string scriptName)
        {
            return string.IsNullOrWhiteSpace(scriptName) ||
                   Player.PlayedScript && scriptName.EqualsFastIgnoreCase(Player.PlayedScript.Name);
        }

        protected virtual void NavigatePlayedScript (string label)
        {
            if (string.IsNullOrEmpty(label)) Player.Play();
            else if (Player.PlayedScript.LabelExists(label)) Player.PlayFromLabel(label);
            else Err($"Failed navigating script playback to `{label}` label: label not found in `{Player.PlayedScript.Name}` script.");
        }

        protected virtual async UniTask PreloadAndNavigateScriptAsync (string scriptName, string label)
        {
            var state = Engine.GetService<IStateManager>();
            if (ShouldResetAll()) await state.ResetStateAsync(dontResetTypes, () => Player.PreloadAndPlayAsync(scriptName, label: label));
            else if (ShouldResetSpecific()) await state.ResetStateAsync(ResetState, () => Player.PreloadAndPlayAsync(scriptName, label: label));
            else await Player.PreloadAndPlayAsync(scriptName, label: label);
        }

        protected virtual bool ShouldResetAll ()
        {
            var config = Engine.GetConfiguration<StateConfiguration>();
            return !Assigned(ResetState) && config.ResetOnGoto ||
                   Assigned(ResetState) && ResetState.Length == 1 && ResetState[0] == ResetAllFlag;
        }

        protected virtual bool ShouldResetSpecific ()
        {
            return Assigned(ResetState) && ResetState.Length > 0 && ResetState[0] != NoResetFlag;
        }

        private static void EnsureDontResetTypesLoaded ()
        {
            if (dontResetTypes is null)
                dontResetTypes = Engine.Types.Where(t => t.IsDefined(typeof(DontResetAttribute), false)).ToArray();
        }
    }
}
