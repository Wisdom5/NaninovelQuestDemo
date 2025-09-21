// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Threading;
using UnityEngine;

namespace Naninovel
{
    public class CancelOnDestroy : MonoBehaviour
    {
        public CancellationToken Token => cts.Token;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private void OnDestroy ()
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
