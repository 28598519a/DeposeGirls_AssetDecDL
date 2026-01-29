using DeposeGirls_AssetDecDL;
using System.IO;
using System.Net.Http;
using System.Text;

public static class WebClient
{
    private static readonly HttpClient client = new();

    /// <summary>
    /// 從指定的網址取得資料 (byte[])
    /// </summary>
    public static async Task<byte[]> GetWebDataAsync(string url)
    {
        return await client.GetByteArrayAsync(url);
    }

    /// <summary>
    /// 從指定的網址取得資料 (string)
    /// </summary>
    public static async Task<string> GetWebStringAsync(string url)
    {
        byte[] response = await client.GetByteArrayAsync(url);
        return Encoding.UTF8.GetString(response);
    }

    /// <summary>
    /// 從指定的網址下載檔案並儲存
    /// </summary>
    public static async Task DownLoadFileAsync(string downloadUrl, string savePath, bool overWrite, CancellationToken ct = default, Action<string>? onError = null, Func<byte[], byte[]>? decode = null)
    {
        string? dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(savePath) && !overWrite)
            return;

        try
        {
            byte[] data = await client.GetByteArrayAsync(downloadUrl, ct);
            if (decode != null)
            {
                byte[]? newdata = decode(data);
                if (newdata != null)
                {
                    data = newdata;
                }
            }
            await File.WriteAllBytesAsync(savePath, data, ct);
            Interlocked.Increment(ref App.glocount);
        }
        catch
        {
            // 沒有的資源直接跳過，並看是否要記錄
            onError?.Invoke(
                downloadUrl + Environment.NewLine +
                savePath + Environment.NewLine
            );
#if DEBUG
            throw;
#endif
        }
    }
}
