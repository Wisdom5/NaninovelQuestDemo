// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel.Spreadsheet
{
    public readonly struct ProcessorProgress
    {
        public readonly string Info;
        public readonly float Progress;

        public ProcessorProgress (string info, float progress)
        {
            Info = info;
            Progress = progress;
        }
    }
}
