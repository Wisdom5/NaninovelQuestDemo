// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ManagedTextConfiguration : Configuration
    {
        /// <summary>
        /// Default managed text document resource loader path prefix.
        /// </summary>
        public const string DefaultPathPrefix = "Text";
        /// <summary>
        /// Name of the category (managed text document local resource path) to use when
        /// no category is specified in <see cref="ManagedTextAttribute"/>.
        /// </summary>
        public const string DefaultCategory = "Uncategorized";
        /// <summary>
        /// Prefix of categories for localized script text map documents.
        /// </summary>
        public const string ScriptMapCategory = "Scripts";
        /// <summary>
        /// Default category for unlockable tip records.
        /// </summary>
        public const string TipCategory = "Tips";

        [Tooltip("Configuration of the resource loader used with the managed text documents.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultPathPrefix };
        [Tooltip("Document categories (local resource paths) for which to use multiline document format.")]
        public string[] MultilineCategories = { $"{ScriptMapCategory}/*", TipCategory };

        /// <summary>
        /// Checks whether specified managed text document category should be formatted in multiline.
        /// </summary>
        public virtual bool IsMultilineCategory (string category)
        {
            return !string.IsNullOrEmpty(category) && MultilineCategories.Any(
                c => c.EndsWithFast("*")
                    ? category.StartsWithFast(c.GetBeforeLast("*"))
                    : c == category
            );
        }
    }
}
