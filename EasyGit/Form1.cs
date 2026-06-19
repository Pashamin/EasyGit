/*
███████╗ █████╗ █▀████╗██╗   ██╗ ██████╗ ██╗████████╗
██╔════╝██╔══██╗██╔════╝╚██╗ ██╔╝██╔════╝ ██║╚══██╔══╝
█████╗  ███████║╚█████╗  ╚████╔╝ ██║  ███╗██║   ██║   
██╔══╝  ██╔══██║ ╚═══██╗  ╚██╔╝  ██║   ██║██║   ██║   
███████╗██║  ██║██████▀╝   ██║   ╚██████╔╝██║   ██║   
╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝    ╚═════╝ ╚═╝   ╚═╝
                                                        
   -> GitHub: https://github.com/Pashamin/EasyGit
   -> Fork Policy: Please credit the original repository.
*/








using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibGit2Sharp;
using System.Net.Http;
using System.Diagnostics;
using System.IO;

namespace EasyGit
{
    public partial class Form1 : Form
    {

        private string currentVersion = "v1.0.3";
        private string downloadUrl = ""; 
        private bool isUpdateAvailable = false;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {

            await CheckForUpdates();
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            if (isUpdateAvailable)
            {
                button1.Enabled = false;
                button1.Text = "Скачивание...";

                await DownloadAndInstallUpdate();
                return;
            }


            string repoUrl = textBox1.Text.Replace(" ", "")
                                          .Replace("\r", "")
                                          .Replace("\n", "");

            if (repoUrl.StartsWith("gitclone"))
            {
                repoUrl = repoUrl.Replace("gitclone", "");
            }

            if (string.IsNullOrEmpty(repoUrl) || repoUrl == "Ссылка на репозиторий...")
            {
                MessageBox.Show("Пожалуйста, введите ссылку на GitHub репозиторий!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!repoUrl.StartsWith("http://") && !repoUrl.StartsWith("https://") && !repoUrl.StartsWith("git@"))
            {
                string translated = TranslateGitError("unsupported url protocol");
                MessageBox.Show(translated, "Ошибка ссылки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите папку, в которую скачется репозиторий";

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string targetFolder = fbd.SelectedPath;

                    try
                    {
                        progressBar1.Style = ProgressBarStyle.Blocks;
                        progressBar1.Value = 0;
                        progressBar1.Maximum = 100;

                        string repoName = repoUrl.Split('/').Last().Replace(".git", "");
                        string finalPath = System.IO.Path.Combine(targetFolder, repoName);

                        CloneOptions options = new CloneOptions();

                        options.FetchOptions.OnTransferProgress = (progress) =>
                        {
                            if (progress.TotalObjects > 0)
                            {
                                int percent = (progress.ReceivedObjects * 100) / progress.TotalObjects;

                                if (progressBar1.IsHandleCreated)
                                {
                                    progressBar1.Invoke((MethodInvoker)delegate
                                    {
                                        if (percent >= 0 && percent <= 100)
                                        {
                                            progressBar1.Value = percent;
                                        }
                                    });
                                }
                            }
                            return true;
                        };

                        await Task.Run(() =>
                        {
                            Repository.Clone(repoUrl, finalPath, options);
                        }).ConfigureAwait(true);

                        progressBar1.Value = 100;
                        MessageBox.Show("Репозиторий успешно скачан!", "Готово!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        progressBar1.Value = 0;

                        if (Properties.Settings.Default.OpenFolderAfterDownload)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", finalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        progressBar1.Style = ProgressBarStyle.Blocks;
                        progressBar1.Value = 0;

                        string rawError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        string russianError = TranslateGitError(rawError);

                        MessageBox.Show(russianError, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task CheckForUpdates()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "EasyGit-Updater");

                    string url = "https://api.github.com/repos/Pashamin/EasyGit/releases/latest";
                    string response = await client.GetStringAsync(url);

                    string latestVersionStr = GetJsonValue(response, "tag_name").Replace("v", ""); 
                    string currentVersionStr = currentVersion.Replace("v", "");
                    downloadUrl = GetJsonValue(response, "browser_download_url");


                    System.Version latestVersion = new System.Version(latestVersionStr);
                    System.Version appVersion = new System.Version(currentVersionStr);


                    if (latestVersion > appVersion && !string.IsNullOrEmpty(downloadUrl))
                    {
                        isUpdateAvailable = true;

                        button1.Invoke((MethodInvoker)delegate {
                            button1.Text = "Обновить EasyGit";
                            button1.BackColor = System.Drawing.Color.DarkOrange;
                        });
                    }
                }
            }
            catch
            {
                
            }
        }

        private string GetJsonValue(string json, string key)
        {
            int pos = json.IndexOf($"\"{key}\":");
            if (pos == -1) return "";


            int start = json.IndexOf("\"", pos + key.Length + 2) + 1;
            if (start == 0) return "";

            int end = json.IndexOf("\"", start);
            if (end == -1) return "";

            string result = json.Substring(start, end - start);


            return result.Replace("\\/", "/");
        }

        private async Task DownloadAndInstallUpdate()
        {
            try
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "EasyGit_Update.zip");
                string currentExePath = Application.ExecutablePath;
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory; 


                using (HttpClient client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(tempZip, bytes);
                }

                button1.Invoke((MethodInvoker)delegate {
                    button1.Text = "Установка...";
                });


                string cmdCommands = $"/c timeout /t 2 /nobreak && " +
                                     $"powershell -Command \"Expand-Archive -Path '{tempZip}' -DestinationPath '{currentDirectory}' -Force\" && " +
                                     $"del \"{tempZip}\" && " +
                                     $"start \"\" \"{currentExePath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdCommands,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(psi);
                Application.Exit(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Упс!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button1.Enabled = true;
                button1.Text = "Обновить EasyGit";
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "Ссылка на репозиторий...")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = "Ссылка на репозиторий...";
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settings = new SettingsForm())
            {
                settings.ShowDialog();
            }
        }

        private string TranslateGitError(string englishError)
        {
            string err = englishError.ToLower();

            if (err.Contains("unsupported url protocol") || err.Contains("protocol"))
            {
                return "🔗 Неподдерживаемый протокол ссылки!\n\n" +
                       "Скорее всего, ссылка введена некорректно.\n" +
                       "Убедитесь, что она начинается с 'https://' и скопирована целиком.\n" +
                       "Пример правильной ссылки:\nhttps://github.com/vlc/vlc.git";
            }

            if (err.Contains("not found") || err.Contains("404"))
            {
                return "❌ Репозиторий не найден!\n\nВозможные причины:\n" +
                       "1. Вы опечатались в ссылке.\n" +
                       "2. Этот репозиторий приватный (доступен только автору).\n" +
                       "3. Автор удалил этот проект с GitHub.";
            }

            if (err.Contains("authentication") || err.Contains("credentials") || err.Contains("401"))
            {
                return "🔒 Ошибка доступа (Нужна авторизация)!\n\n" +
                       "Этот репозиторий закрытый. EasyGit пока поддерживает только публичные проекты. " +
                       "Убедитесь, что ссылка ведет на открытый репозиторий.";
            }

            if (err.Contains("could not resolve host") || err.Contains("connection") || err.Contains("timeout"))
            {
                return "🌐 Проблема с интернетом!\n\n" +
                       "Программа не может связаться с GitHub. Проверьте свое сетевое подключение, " +
                       "отключите VPN (иногда они блокируют гит) или попробуйте позже.";
            }

            if (err.Contains("exists and is not an empty directory") || err.Contains("already exists"))
            {
                return "📁 Папка уже занята!\n\n" +
                       "В выбранном месте уже существует папка с точно таким же именем, " +
                       "и она не пуста. Выберите другую папку для скачивания.";
            }

            if (err.Contains("invalid url") || err.Contains("is not valid"))
            {
                return "🔗 Сломанная ссылка!\n\n" +
                       "Введенный текст вообще не похож на рабочую ссылку. " +
                       "Скопируйте правильный адрес из браузера (например: https://github.com/user/repo).";
            }

            return $"⚠️ Неизвестная ошибка Git:\n\n{englishError}";
        }
    }
}