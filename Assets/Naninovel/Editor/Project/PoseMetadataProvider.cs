// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Provides actor poses IDE metadata for autocompletion.
    /// </summary>
    public class PoseMetadataProvider : IMetadataProvider
    {
        public Project GetMetadata (MetadataOptions options)
        {
            var constants = new List<Constant>();

            options.NotifyProgress("Processing character poses...", 0);
            var chars = ProjectConfigurationProvider.LoadOrDefault<CharactersConfiguration>();
            constants.Add(CreatePoseConstant(Constants.CharacterType, Constants.WildcardType, chars.SharedPoses.Select(p => p.Name)));
            foreach (var kv in chars.Metadata.ToDictionary())
                if (kv.Value.Poses.Count > 0)
                    constants.Add(CreatePoseConstant(Constants.CharacterType, kv.Key, kv.Value.Poses.Select(p => p.Name)));

            options.NotifyProgress("Processing background poses...", 0.5f);
            var backs = ProjectConfigurationProvider.LoadOrDefault<BackgroundsConfiguration>();
            constants.Add(CreatePoseConstant(Constants.BackgroundType, Constants.WildcardType, backs.SharedPoses.Select(p => p.Name)));
            foreach (var kv in backs.Metadata.ToDictionary())
                if (kv.Value.Poses.Count > 0)
                    constants.Add(CreatePoseConstant(Constants.BackgroundType, kv.Key, kv.Value.Poses.Select(p => p.Name)));

            return new Project { Constants = constants.ToArray() };
        }

        private Constant CreatePoseConstant (string actorType, string actorId, IEnumerable<string> poses)
        {
            var name = $"Poses/{actorType}/{actorId}";
            return new Constant { Name = name, Values = poses.ToArray() };
        }
    }
}
