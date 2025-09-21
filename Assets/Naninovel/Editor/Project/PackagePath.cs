// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.IO;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides paths to various package-related folders and resources. All the returned paths are in absolute format.
    /// </summary>
    public static class PackagePath
    {
        public static string PackageRootPath => GetPackageRootPath();
        public static string PackageMarkerPath => Path.Combine(cachedPackageRootPath, marker);
        public static string EditorResourcesPath => Path.Combine(PackageRootPath, "Editor/Resources/Naninovel");
        public static string RuntimeResourcesPath => Path.Combine(PackageRootPath, "Resources/Naninovel");
        public static string PrefabsPath => Path.Combine(PackageRootPath, "Prefabs");
        public static string GeneratedDataPath => GetGeneratedDataPath();

        private const string marker = "Elringus.Naninovel.Editor.asmdef";
        private static string cachedPackageRootPath;

        private static string GetPackageRootPath ()
        {
            if (string.IsNullOrEmpty(cachedPackageRootPath) || !File.Exists(PackageMarkerPath))
                cachedPackageRootPath = FindRootInAssets();
            return cachedPackageRootPath ?? throw new Error("Failed to find Naninovel package folder.");
        }

        private static string FindRootInAssets ()
        {
            foreach (var path in Directory.EnumerateFiles(Application.dataPath, marker, SearchOption.AllDirectories))
                return Directory.GetParent(path)?.Parent?.FullName;
            return null;
        }

        // TODO: Switch to UPM distribution once Asset Store supports it.
        // private static string FindRootInPackages ()
        // {
        //     try { return Path.GetFullPath("Packages/com.elringus.naninovel"); }
        //     catch { return null; }
        // }

        private static string GetGeneratedDataPath ()
        {
            var localPath = ProjectConfigurationProvider.LoadOrDefault<EngineConfiguration>().GeneratedDataPath;
            var path = PathUtils.Combine(Application.dataPath, localPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}
