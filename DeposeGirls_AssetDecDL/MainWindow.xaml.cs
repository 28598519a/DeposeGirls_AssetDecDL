using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace DeposeGirls_AssetDecDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static async Task<string> GetServerBaseInfoAsync(int device = 0)
        {
            string ver = String.Empty;
            string response = String.Empty;

            try
            {
                switch (device)
                {
                    case 0:
                        response = (await WebClient.GetWebStringAsync(App.ServerURL_DMM)).Replace(" ", "");
                        var match = Regex.Match(response, @"window\.VUE_APP_BYTE_PATH='([^']+)'");

                        if (match.Success)
                            App.BaseUrl = match.Groups[1].Value + "/game/webgl/webgl-release/Desktop";

                        response = await WebClient.GetWebStringAsync(App.ServerURL_Web);
                        break;
                    case 1:
                        response = await WebClient.GetWebStringAsync(App.ServerURL_Android);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message.ToString());
#if DEBUG
                throw;
#else
                return String.Empty;
#endif
            }

            // 處理Server回應的json內容
            try
            {
                JsonElement JsonString = JsonDocument.Parse(response).RootElement;
                ver = JsonString.GetProperty("ab").GetInt32().ToString();

                if (device == 1)
                    App.BaseUrl = JsonString.GetProperty("au").GetProperty("FtpPath__Android").GetString();
            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show(ex.Message.ToString());
#if DEBUG
                throw;
#endif
            }
            finally
            {
                if (String.IsNullOrEmpty(App.BaseUrl) || String.IsNullOrEmpty(ver))
                {
                    System.Windows.MessageBox.Show(
                        $"BaseUrl: {App.BaseUrl}" + Environment.NewLine +
                        $"ver: {ver}", "Detected empty result");
                }
            }
            return ver;
        }

        private async void Btn_download_list_Click(object sender, RoutedEventArgs e)
        {
            btn_download_list.IsEnabled = false;
            lb_counter.Content = "AssetList.txt下載中";

            // 覆蓋原有的assetlist
            string AssetList = Path.Combine(App.Root, "AssetList.txt");
            // 0: Web, 1: Android
            int device = cb_devices.SelectedIndex;
            string ver = await GetServerBaseInfoAsync(device);

            if (String.IsNullOrEmpty(ver) || String.IsNullOrEmpty(App.BaseUrl))
            {
                btn_download_list.IsEnabled = true;
                lb_counter.Content = String.Empty;
                return;
            }

            // Get Resources list
            try
            {
                byte[]? bin = null;
                if (device == 0)
                    bin = await WebClient.GetWebDataAsync($"{App.BaseUrl}/webgl-release/abfiles{ver}");
                else
                    bin = await WebClient.GetWebDataAsync($"{App.BaseUrl}/assets/abfiles{ver}");
                byte[] decompressed = UnZip.LZ4Decode(bin);
                File.WriteAllBytes(AssetList, decompressed);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message.ToString());
#if DEBUG
                throw;
#else
                btn_download_list.IsEnabled = true;
                lb_counter.Content = String.Empty;
                return;
#endif
            }

            btn_download_list.IsEnabled = true;
            lb_counter.Content = String.Empty;
            System.Windows.MessageBox.Show("Asset list 下載完成", "Finish");
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        /// <summary>
        /// AssetList結構
        /// </summary>
        private class FileInfo
        {
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
            public string Size { get; set; } = "";
        }

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            // Load Res list
            Microsoft.Win32.OpenFileDialog openFileDialog = new();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "AssetList.txt|*.txt";
            if (!openFileDialog.ShowDialog() == true)
                return;

            if (!Directory.Exists(App.Respath))
                Directory.CreateDirectory(App.Respath);

            if (String.IsNullOrEmpty(App.BaseUrl))
            {
                if(String.IsNullOrEmpty(await GetServerBaseInfoAsync()))
                    return;
                else if (String.IsNullOrEmpty(App.BaseUrl))
                    return;
            }

            btn_download.IsEnabled = false;

            List<string> FileList = File.ReadLines(openFileDialog.FileName).ToList();
            Dictionary<string, FileInfo> AssetList = new();
            string directory = "";
            int endIndex = -1;

            for (int i = 0; i < FileList.Count - 2; i++)
            {
                string line = FileList[i];

                // 處理檔案行
                if (directory.Length > 0 && i <= endIndex && line.Contains('|'))
                {
                    string[] cut = line.Split('|');

                    if (cut.Length >= 4)
                    {
                        string name = cut[0];
                        string u = cut[2];
                        string size = cut[3];

                        AssetList[name] = new FileInfo
                        {
                            Name = name,
                            Url = $"{App.BaseUrl}/{directory}/{u}{name}",
                            Size = size
                        };
                    }

                    // 區塊結束
                    if (i == endIndex)
                    {
                        directory = "";
                        endIndex = -1;
                    }

                    continue;
                }

                // 偵測目錄區塊起點
                if (!line.Contains('|') &&
                    int.TryParse(FileList[i + 1], out int count) &&
                    count > 0 &&
                    FileList[i + 2].Contains('|') &&
                    FileList[i + 1 + count].Contains('|'))
                {
                    directory = line;
                    endIndex = i + 1 + count;
                }
            }

            App.TotalCount = AssetList.Count;
            int CurrentCount = 0;
            bool isCover = cb_isCover.IsChecked == true;
            bool isDebug = cb_Debug.IsChecked == true;

            await Parallel.ForEachAsync(
                AssetList.Values,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = pool
                },
                async (asset, ct) =>
                {
                    string path = Path.Combine(App.Respath, asset.Name);
                    await WebClient.DownLoadFileAsync(asset.Url, path, isCover, ct,
                        error =>
                        {
                            if (isDebug)
                                App.log.Add(error);
                        },
                        decode: DecryptAsset.Jdn_Decrypt);

                    // thread-safe count
                    int current = Interlocked.Increment(ref CurrentCount);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        lb_counter.Content = $"進度 : {current} / {App.TotalCount}";
                    });
                });

            if (cb_Debug.IsChecked == true && App.log.Count > 0)
            {
                using (StreamWriter outputFile = new StreamWriter("Fail.log", false))
                {
                    foreach (string s in App.log)
                        outputFile.WriteLine(s);
                }
            }

            string msg = $"下載完成，共{App.glocount}個檔案";
            if (App.log.Count > 0)
                msg += $"，{App.log.Count}個檔案失敗";
            if (App.TotalCount - App.glocount > 0)
                msg += $"，略過{App.TotalCount - App.glocount - App.log.Count}個檔案";

            btn_download.IsEnabled = true;
            lb_counter.Content = String.Empty;
            System.Windows.MessageBox.Show(msg, "Finish");
        }

        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            string selectPath = String.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = App.Root;

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectPath = openFolderDialog.Folder;
                if (!Directory.Exists(selectPath))
                {
                    selectPath = String.Empty;
                    lb_counter.Content = "Error: 選擇的路徑不存在";
                }
            }

            var result = System.Windows.MessageBox.Show("轉換將會覆蓋掉原始檔案，繼續?", "注意", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            int count = 0;
            List<string> fileList = Directory.GetFiles(selectPath, "*", SearchOption.AllDirectories).ToList();

            foreach (string file in fileList)
            {
                byte[] data = File.ReadAllBytes(file);
                byte[]? newdata = DecryptAsset.Jdn_Decrypt(data);

                if (newdata == null) continue;
                else
                {
                    File.WriteAllBytes(file, newdata);
                    count++;
                }
            }

            lb_counter.Content = $"已轉換 {count} 個檔案";
        }
    }
}