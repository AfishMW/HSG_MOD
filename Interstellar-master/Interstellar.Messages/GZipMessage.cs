using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages;

static public class GZipMessage
{
    static public byte[] CompressString(string text) {
        byte[] inputBytes = Encoding.UTF8.GetBytes(text);

        using (var outputStream = new MemoryStream())
        {
            using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gZipStream.Write(inputBytes, 0, inputBytes.Length);
            }
            return outputStream.ToArray();
        }
    }

    static public string DecompressString(byte[] bytes)
    {
        using (var inputStream = new MemoryStream(bytes))
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    gZipStream.CopyTo(outputStream);
                }
                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
        }
    }
}
