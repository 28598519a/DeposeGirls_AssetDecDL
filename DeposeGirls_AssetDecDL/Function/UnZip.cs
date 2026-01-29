using K4os.Compression.LZ4.Streams;
// using ICSharpCode.SharpZipLib.Core;
// using ICSharpCode.SharpZipLib.Zip;
using System.IO;

public static class UnZip
{
    /// <summary>
    /// LZ4解壓縮
    /// </summary>
    public static byte[] LZ4Decode(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var lz4 = LZ4Stream.Decode(input);
        using var output = new MemoryStream();
        lz4.CopyTo(output);
        return output.ToArray();
    }

    /*
    /// <summary>
    /// 檢查是否為加密壓縮檔
    /// </summary>
    public static bool IsEncryptZip(string path, string password = "")
    {
        bool isenc = false;
        using (FileStream fileStreamIn = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (ZipInputStream zipInStream = new ZipInputStream(fileStreamIn))
        {
            ZipEntry entry;
            if (password != null && password != string.Empty) zipInStream.Password = password;
            while ((entry = zipInStream.GetNextEntry()) != null)
            {
                if (entry.IsCrypted) isenc = true;
            }
            return isenc;
        }
    }

    /// <summary>
    /// ZIP解壓縮
    /// </summary>
    public void UnZipFiles(string filepath, string destfolder, string password = "")
    {
        ZipInputStream zipInStream = null;

        try
        {
            if (!Directory.Exists(destfolder))
                Directory.CreateDirectory(destfolder);

            zipInStream = new ZipInputStream(File.OpenRead(filepath));
            if (password != null && password != string.Empty) zipInStream.Password = password;
            ZipEntry entry;

            while ((entry = zipInStream.GetNextEntry()) != null)
            {
                string filePath = Path.Combine(destfolder, entry.Name);

                if (entry.Name != "")
                {
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Skip directory entry
                    if (Path.GetFileName(filePath).Length == 0)
                    {
                        continue;
                    }

                    byte[] buffer = new byte[4096];
                    using (FileStream streamWriter = File.Create(filePath))
                    {
                        StreamUtils.Copy(zipInStream, streamWriter, buffer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine(ex.Message);
        }
        finally
        {
            zipInStream.Close();
            zipInStream.Dispose();
        }
    }
    */
}
