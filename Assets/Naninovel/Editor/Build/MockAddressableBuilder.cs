// Copyright 2023 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    public class MockAddressableBuilder : IAddressableBuilder
    {
        public void RemoveEntries () { }
        public bool TryAddEntry (string assetGuid, string resourcePath) => false;
        public void BuildContent () { }
    }
}
