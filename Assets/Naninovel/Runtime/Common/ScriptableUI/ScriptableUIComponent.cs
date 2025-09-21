// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine.EventSystems;

namespace Naninovel
{
    public abstract class ScriptableUIComponent<T> : ScriptableUIBehaviour where T : UIBehaviour
    {
        public virtual T UIComponent => uiComponent ? uiComponent : uiComponent = GetComponent<T>();

        private T uiComponent;
    }
}
