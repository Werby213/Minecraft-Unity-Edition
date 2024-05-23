using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

public static class SerializationHelper
{
    public static byte[] SerializeToBinary(object obj)
    {
        if (obj == null)
            return null;

        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static object DeserializeFromBinary(byte[] data)
    {
        if (data == null)
            return null;

        using (MemoryStream ms = new MemoryStream(data))
        {
            BinaryFormatter bf = new BinaryFormatter();
            return bf.Deserialize(ms);
        }
    }

    public static byte[] Compress(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }
    }

    public static byte[] Decompress(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
            {
                using (MemoryStream decompressed = new MemoryStream())
                {
                    gzip.CopyTo(decompressed);
                    return decompressed.ToArray();
                }
            }
        }
    }
}
