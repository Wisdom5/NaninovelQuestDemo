// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;

namespace Naninovel
{
    /// <summary>
    /// Annotations (comment strings) associated with localizable text identifiers.
    /// </summary>
    public class LocalizableTextAnnotations
    {
        // First localizable text ID in script line to comment line above.
        private readonly LiteralMap<string> textIdToComment = new LiteralMap<string>();

        /// <summary>
        /// Finds annotations in specified scenario script.
        /// </summary>
        public static LocalizableTextAnnotations FromScript (Script script)
        {
            var annotations = new LocalizableTextAnnotations();
            var list = new ScriptPlaylist(script);
            foreach (var comment in script.Lines.OfType<CommentScriptLine>())
                if (!string.IsNullOrWhiteSpace(comment.CommentText) &&
                    list.GetCommandAfterLine(comment.LineIndex, 0) is Command.ILocalizable command &&
                    command.GetType().GetFields().FirstOrDefault(f => f.FieldType == typeof(LocalizableTextParameter))?.GetValue(command) is LocalizableTextParameter param &&
                    param.RawValue?.Parts.FirstOrDefault(p => p.Kind == ParameterValuePartKind.IdentifiedText).Id is string id)
                    annotations.textIdToComment[id] = comment.CommentText;
            return annotations;
        }

        /// <summary>
        /// Attempts to get annotation associated with specified localizable text identifier.
        /// </summary>
        public bool TryGet (string textId, out string annotation) => textIdToComment.TryGetValue(textId, out annotation);
    }
}
