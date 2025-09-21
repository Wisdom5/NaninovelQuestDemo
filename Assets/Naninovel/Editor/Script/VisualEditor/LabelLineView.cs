// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class LabelLineView : ScriptLineView
    {
        public readonly LineTextField ValueField;

        public LabelLineView (int lineIndex, string lineText, VisualElement container)
            : base(lineIndex, container)
        {
            var value = lineText.GetAfterFirst(Parsing.Identifiers.LabelLine)?.TrimFull();
            ValueField = new LineTextField(Parsing.Identifiers.LabelLine, value);
            Content.Add(ValueField);
        }

        public override string GenerateLineText () => $"{Parsing.Identifiers.LabelLine} {ValueField.value}";
    }
}
