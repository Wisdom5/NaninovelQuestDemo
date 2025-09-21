// Copyright 2023 ReWaffle LLC. All rights reserved.

using Naninovel.Parsing;

namespace Naninovel
{
    public class CommentLineParser : ScriptLineParser<CommentScriptLine, CommentLine>
    {
        protected override CommentScriptLine Parse (CommentLine lineModel)
        {
            return new CommentScriptLine(lineModel.Comment.Text, LineIndex, LineHash);
        }
    }
}
