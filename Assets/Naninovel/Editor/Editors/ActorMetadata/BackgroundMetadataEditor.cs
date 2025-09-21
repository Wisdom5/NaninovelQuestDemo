// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using UnityEditor;

namespace Naninovel
{
    public class BackgroundMetadataEditor : OrthoMetadataEditor<IBackgroundActor, BackgroundMetadata>
    {
        protected override Action<SerializedProperty> GetCustomDrawer (string propertyName)
        {
            switch (propertyName)
            {
                case nameof(BackgroundMetadata.MatchMode): return DrawWhen(!IsGeneric);
                case nameof(BackgroundMetadata.CustomMatchRatio): return DrawWhen(!IsGeneric && Metadata.MatchMode == AspectMatchMode.Custom);
                case nameof(CharacterMetadata.Poses): return DrawWhen(HasResources, ActorPosesEditor.Draw);
            }
            return base.GetCustomDrawer(propertyName);
        }
    }
}
