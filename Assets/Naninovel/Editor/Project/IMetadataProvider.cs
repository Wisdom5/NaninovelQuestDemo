// Copyright 2023 ReWaffle LLC. All rights reserved.

using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to provide various project-specific metadata
    /// for external tools (IDE extension, web editor, etc).
    /// </summary>
    /// <remarks>
    /// Implementation is expected to have parameterless constructor.
    /// All implementations are executed when generating metadata and the results are merged.
    /// </remarks>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Returns project metadata.
        /// </summary>
        Project GetMetadata (MetadataOptions options);
    }
}
