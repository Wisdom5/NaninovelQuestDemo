// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Thrown upon cancellation of an async operation via <see cref="AsyncToken"/>.
    /// </summary>
    public class AsyncOperationCanceledException : OperationCanceledException
    {
        public AsyncOperationCanceledException (AsyncToken asyncToken)
            : base(asyncToken.CancellationToken)
        {
            if (!asyncToken.Canceled) throw new ArgumentException("Provided token is not canceled.", nameof(asyncToken));
        }

        protected AsyncOperationCanceledException () { }
    }
}
