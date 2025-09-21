// Copyright 2023 ReWaffle LLC. All rights reserved.


namespace Naninovel
{
    /// <summary>
    /// Implementation is able to convert exported google drive files to <typeparamref name="TResult"/>.
    /// </summary>
    public interface IGoogleDriveConverter<TResult> : IRawConverter<TResult>
    {
        string ExportMimeType { get; }
    }
}
