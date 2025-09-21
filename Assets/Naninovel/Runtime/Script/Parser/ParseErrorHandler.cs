// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel
{
    public class ParseErrorHandler : IErrorHandler
    {
        public ICollection<ScriptParseError> Errors { get; set; }
        public int LineIndex { get; set; }

        public void HandleError (ParseError error)
        {
            Errors?.Add(new ScriptParseError(LineIndex, error.Message));
        }
    }
}
