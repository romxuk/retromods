using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.RomFs;
using LibHac.Util;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Ryujinx.HLE.Loaders.Mods
{
    internal static class RetroModsRomFs
    {
        private const ulong Mario35 = 0x0100277011F1A000;

        internal static bool Matches(ulong titleId, string gameVersion, string patchVersion) =>
            titleId == Mario35 && !string.IsNullOrWhiteSpace(patchVersion) &&
            string.Equals(gameVersion, patchVersion.Trim(), StringComparison.Ordinal);

        public static IStorage Apply(ulong titleId, string gameVersion, IStorage baseStorage)
        {
            if (titleId != Mario35)
            {
                return baseStorage;
            }

            var assembly = typeof(RetroModsRomFs).Assembly;
            using Stream archiveStream = assembly.GetManifestResourceStream("RetroMods.Mario35.romfs.zip");
            if (archiveStream == null)
            {
                return baseStorage;
            }

            using Stream versionStream = assembly.GetManifestResourceStream("RetroMods.Mario35.version.txt");
            using StreamReader versionReader = versionStream == null ? null : new StreamReader(versionStream);
            if (!Matches(titleId, gameVersion, versionReader?.ReadToEnd()))
            {
                Logger.Warning?.Print(LogClass.ModLoader, $"Bundled Mario 35 RomFS patch does not support game version {gameVersion}; skipped.");
                return baseStorage;
            }

            using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read);
            HashSet<string> replacements = new(StringComparer.Ordinal);
            RomFsBuilder builder = new();

            foreach (ZipArchiveEntry entry in archive.Entries.OrderBy(entry => entry.FullName, StringComparer.Ordinal))
            {
                if (entry.FullName.EndsWith('/'))
                {
                    continue;
                }

                // Archive entries are paths relative to the RomFS root, never host paths.
                string[] segments = entry.FullName.Split('/');
                if (entry.FullName.Contains('\\') || segments.Any(segment => segment is "" or "." or ".."))
                {
                    throw new InvalidDataException($"Invalid bundled RomFS path: {entry.FullName}");
                }

                string path = "/" + entry.FullName;
                if (!replacements.Add(path))
                {
                    throw new InvalidDataException($"Duplicate bundled RomFS path: {path}");
                }

                using Stream input = entry.Open();
                MemoryStream content = new();
                input.CopyTo(content);
                content.Position = 0;
                builder.AddFile(path, content.AsStorage().AsFile(OpenMode.Read));
            }

            if (replacements.Count == 0)
            {
                return baseStorage;
            }

            RomFsFileSystem baseRom = new(baseStorage);
            foreach (DirectoryEntryEx entry in baseRom.EnumerateEntries()
                         .Where(entry => entry.Type == DirectoryEntryType.File && !replacements.Contains(entry.FullPath))
                         .OrderBy(entry => entry.FullPath, StringComparer.Ordinal))
            {
                using UniqueRef<IFile> file = new();
                baseRom.OpenFile(ref file.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                builder.AddFile(entry.FullPath, file.Release());
            }

            Logger.Info?.Print(LogClass.ModLoader, $"Automatically applied {replacements.Count} bundled Mario 35 RomFS files for {gameVersion}.");
            return builder.Build();
        }
    }
}
