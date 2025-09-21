// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent a choice handler actor on scene.
    /// </summary>
    public interface IChoiceHandlerActor : IActor
    {
        /// <summary>
        /// List of the currently available options to choose from,
        /// in the same order the options were added.
        /// </summary>
        List<ChoiceState> Choices { get; }

        /// <summary>
        /// Fetches a choice state with the specified ID.
        /// </summary>
        ChoiceState GetChoice (string id);
        /// <summary>
        /// Adds an option to choose from.
        /// </summary>
        void AddChoice (ChoiceState choice);
        /// <summary>
        /// Removes a choice option with the specified ID.
        /// </summary>
        void RemoveChoice (string id);
        /// <summary>
        /// Selects a choice option with the specified ID.
        /// </summary>
        void HandleChoice (string id);
    }
}
