using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar;

public static class InterstellarLoader
{
    private static byte[] ReadBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
    public static void Load()
    {
        var zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Libs.zip");
        if (zipStream == null) throw new ArgumentException("Failed to find Interstellar resources.");
        var archive = new ZipArchive(zipStream);
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".dll")) continue;
            using var dllStream = entry.Open();
            Assembly.Load(ReadBytes(dllStream));
        }
    }
}
