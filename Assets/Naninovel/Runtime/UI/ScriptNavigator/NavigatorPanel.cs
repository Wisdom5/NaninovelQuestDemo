// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public abstract class NavigatorPanel : CustomUI
    {
        protected virtual Transform ButtonsContainer => buttonsContainer;
        protected virtual GameObject PlayButtonPrototype => playButtonPrototype;

        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private GameObject playButtonPrototype;

        protected virtual IScriptPlayer Player { get; private set; }
        protected virtual IScriptManager ScriptManager { get; private set; }
        protected abstract IReadOnlyCollection<Script> Scripts { get; }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(ButtonsContainer, PlayButtonPrototype);
            Player = Engine.GetService<IScriptPlayer>();
            ScriptManager = Engine.GetService<IScriptManager>();
            GenerateScriptButtons(Scripts.Select(s => s.Name));
        }

        protected override void OnEnable ()
        {
            base.OnEnable();
            Player.OnPlay += HandlePlay;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            if (Player != null)
                Player.OnPlay -= HandlePlay;
        }

        protected virtual void GenerateScriptButtons (IEnumerable<string> scriptNames)
        {
            if (ButtonsContainer)
                ObjectUtils.DestroyAllChildren(ButtonsContainer);

            foreach (var name in scriptNames)
            {
                var scriptButton = Instantiate(PlayButtonPrototype, ButtonsContainer, false);
                scriptButton.GetComponent<NavigatorPlayButton>().Initialize(this, name, Player);
            }
        }

        private void HandlePlay (Script script)
        {
            if (ScriptManager.Configuration.TitleScript != script.Name)
                Hide();
        }
    }
}
