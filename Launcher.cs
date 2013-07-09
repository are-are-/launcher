using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;
using Ionic.Zip;
using System.IO;
using System.Reflection;

namespace Minecraft_Launcher_by_Gaius_v2
{
    public partial class Launcher : Form
    {
        public static Dictionary<string, string> Settings = new Dictionary<string, string>();
        public static string pathDirGaius = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"\\.gaius";
        public static bool processFree = true;

        public Launcher()
        {
            InitializeComponent();
            Log("Inicjalizacja programu...");
            panel1.BackColor = Color.FromArgb(150, Color.White);
        }



        private void Launcher_Load(object sender, EventArgs e)
        {

            string tempAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Settings.Add("pathDirAppdata", tempAppData);
            Settings.Add("pathDirGaius", tempAppData + "\\.gaius");
            Settings.Add("pathDirMinecraft", tempAppData + "\\.minecraft");
            Settings.Add("versionCurrent", new WebClient().DownloadString("http://mcpl.eu/launcher/version"));
            button3.Text= "Pobierz najnowsz¹ wersjê ("+Settings["versionCurrent"]+")";

            Dictionary<string, bool> Checked = Check();

            if (!Checked["gaius"])
            {
                InitializeFull(() => {
                    if (!Checked["assets"])
                    {
                        DownloadAssets(() =>
                        {
                            UpdateMinecraft();
                            Ready();
                            Unlock(true);
                        });
                    }
                    else
                    {
                        Settings.Add("pathAssets", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\assets");
                        UpdateMinecraft();
                        Ready();
                        Unlock(true);
                    }
                }); 
            }
            else
            {
                string options = GetOptions();
                if (options != null) textBox2.Text = options;
                else textBox2.Text = "-Xmx1G";

                if (!Checked["assets"])
                {
                    DownloadAssets(null);
                    Ready();
                    Unlock(true);
                }
                else
                {
                    Settings.Add("pathAssets", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\assets");
                    Ready();
                    Unlock(true);
                }
            }
        }

        public void Unlock(bool unlock)
        {
            if (unlock)
            {
                textBox1.Enabled = true;
                button1.Enabled = true;
                button3.Enabled = true;
            }
            else
            {
                textBox1.Enabled = false;
                button1.Enabled = false;
                button3.Enabled = false;
            }
        }

        public bool Ready()
        {
            ListVersions();
            string [] a = GetLastNicks();
            if(a!=null) textBox1.Items.AddRange(a);
            return true;
        }

        private void ListVersions()
        {
            DirectoryInfo dirVersions = new DirectoryInfo(pathDirGaius + "\\jar");
            FileInfo[] fileList = dirVersions.GetFiles();
            List<string> versionList = new List<string>();
            foreach (FileInfo fi in fileList)
            {
                if (fi.Extension == ".jar")
                {
                    if (File.Exists(pathDirGaius + "\\jar\\" + fi.Name.Replace(".jar", ".txt")))
                    {
                        versionList.Add(fi.Name.Replace(fi.Extension, ""));
                    }
                }
            }
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(versionList.ToArray());
            comboBox1.SelectedIndex = 0;
        }

        private string GetOptions()
        {
            if (File.Exists(pathDirGaius + "\\options"))
            {
                return File.ReadAllText(pathDirGaius + "\\options");
            }
            else
            {
                return null;
            }
        }

        private string[] GetLastNicks()
        {
            if (File.Exists(pathDirGaius + "\\lastlogin"))
            {
                return File.ReadAllLines(pathDirGaius + "\\lastlogin");
            }
            else
            {
                return null;
            }
        }

        private void AddLastNick(string nick)
        {
            if (nick != null)
            {
                File.AppendAllText(pathDirGaius + "\\lastlogin", nick+Environment.NewLine);
            }
        }

        private void UpdateMinecraft()
        {
            processFree = false;
            Log("Pobieram najnowsz¹ wersjê pliku JAR ("+Settings["versionCurrent"]+").");
            DownloadLast(() =>
            {
                Log("Pobrano.");
                Log("Rozpakowujê...");
                UnzipFile(pathDirGaius + "\\temp\\temp.zip", pathDirGaius + "\\temp", (sendere, ere) => { pbarMain.Value = ere.ProgressPercentage; }, (sendere, ere) =>
                {
                    pbarMain.Value = 0;
                    Log("Rozpakowano.");
                    CopyDir(pathDirGaius + "\\temp\\lib", pathDirGaius + "\\lib");
                    Log("Przenoszê pliki tymczasowe...");
                    string random = Path.GetRandomFileName().Replace(".", "");
                    File.Move(pathDirGaius + "\\temp\\" + Settings["versionCurrent"] + ".jar", pathDirGaius + "\\jar\\" + Settings["versionCurrent"] + "-" + random + ".jar");
                    File.Move(pathDirGaius + "\\temp\\" + Settings["versionCurrent"] + ".txt", pathDirGaius + "\\jar\\" + Settings["versionCurrent"] + "-" + random + ".txt");
                    Log("Przeniesiono.");
                    Log("Usuwam pliki tymczasowe...");
                    new FileInfo(pathDirGaius + "\\temp\\temp.zip").Delete();
                    new DirectoryInfo(pathDirGaius + "\\temp\\lib").Delete(true);
                    Log("Usuniêto.");
                    Log("Aktualizacja zakoñczona.");
                    processFree = true;
                    ListVersions();
                });
            });
        }

        private void InitializeFull(Action callback)
        {
            processFree = false;
            Log("Inicjalizacja rozpoczêta.");
            pbarMain.Value = 0; 
            Log("Tworzê katalogi...");
            Initialize();
            Log("Utworzono katalogi!");
            Log("Pobieram biblioteki...");
            DownloadNatives(() =>
            {
                Log("Pobrano biblioteki.");
                Log("Rozpakowujê biblioteki...");
                pbarMain.Value = 0;
                UnzipFile(pathDirGaius + "\\temp\\natives.zip", pathDirGaius + "\\bin", (sendera, era) => { pbarMain.Value = era.ProgressPercentage; }, (senderi, eri) =>
                {
                    pbarMain.Value = 0;
                    Log("Rozpakowano.");
                    Log("Usuwam pliki tymczasowe...");
                    new FileInfo(pathDirGaius + "\\temp\\natives.zip").Delete();
                    Log("Usuniêto.");
                    Log("Inicjalizacja zakoñczona.");
                    processFree = true;
                    if (callback != null) callback();
                });
            });

        }

        private void LaunchMinecraft(string nick, string options)
        {
            List<string> librariesPathList = new List<string>();
            string version = (string)comboBox1.SelectedItem;
            if (version != null && nick != null)
            {
                string[] a = GetLastNicks();
                if (a != null)
                {
                    if (!a.Contains(nick)) AddLastNick(nick);
                }
                else
                {
                    AddLastNick(nick);
                }

                File.WriteAllText(pathDirGaius+"\\options",textBox2.Text);
                string[] librariesList = File.ReadAllLines(pathDirGaius + "\\jar\\" + version + ".txt");
                foreach (string library in librariesList)
                {
                    librariesPathList.Add(pathDirGaius + "\\lib\\" + library);
                }
                string makeLibraryString = string.Join(";", librariesPathList);
                makeLibraryString += ";" + pathDirGaius + "\\jar\\" + version + ".jar";


                string psi = string.Format(@"{0} -Djava.library.path={1}\bin -cp {2} net.minecraft.client.main.Main --username {3} --session {4} --version 1.6.1 --gameDir {5} --assetsDir {6}",
                options, //EXECUTE OPTIONS
                pathDirGaius, //NATIVES FOLDER
                makeLibraryString, //LIBRARY STRING
                nick, //NICK
                "7ae9007b9909de05ea58e94199a33b30c310c69c", //SSID
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft", //GAMEDIR
                Settings["pathAssets"]); //ASSETSDIR


                Log("Odpalam minecrafta z argumentami: " + psi);
                System.Diagnostics.ProcessStartInfo pinfo;
                string uruchom = checkBox2.Checked ? "java" : "javaw";
                pinfo = new System.Diagnostics.ProcessStartInfo(uruchom, psi);
                System.Diagnostics.Process.Start(pinfo);
                if (checkBox4.Checked) Environment.Exit(0);
            }
            else
            {
                Log("Wybierz wersjê z listy!");
            }
        }


        public Dictionary<string, bool> Check()
        {
            Dictionary<string, bool> Result = new Dictionary<string, bool>();
            if (Directory.Exists(Settings["pathDirMinecraft"]))
            {
                Result.Add("minecraft", true);
                if (Directory.Exists(Settings["pathDirMinecraft"] + "\\assets")) Result.Add("assets", true);
                else Result.Add("assets", false);
            }
            else
            {
                Result.Add("minecraft", false);
                Result.Add("assets", false);
            }

            if (Directory.Exists(pathDirGaius)) Result.Add("gaius", true);
            else Result.Add("gaius", false);

            return Result;
        }

        public void Initialize()
        {
            Directory.CreateDirectory(pathDirGaius);
            Directory.CreateDirectory(pathDirGaius + "\\lib");
            Directory.CreateDirectory(pathDirGaius + "\\bin");
            Directory.CreateDirectory(pathDirGaius + "\\temp");
            Directory.CreateDirectory(pathDirGaius + "\\jar");
            Directory.CreateDirectory(pathDirGaius + "\\assets");
        }

        public void DownloadAssets(Action callback)
        {
            pbarMain.Value = 0;
            Log("Pobieram pliki Ÿród³owe.");
            DownloadFile("http://mcpl.eu/launcher/assets.zip", pathDirGaius + "\\temp\\assets.zip", (sender, e) => { pbarMain.Value = e.ProgressPercentage; }, (sender, e) =>
            {
                Log("Pobrano.");
                Log("Rozpakowujê...");
                UnzipFile(pathDirGaius + "\\temp\\assets.zip", pathDirGaius + "\\assets", (sendera, era) => { pbarMain.Value = era.ProgressPercentage; }, (senderi, eri) =>
                {
                    Log("Rozpakowano.");
                    pbarMain.Value = 0;
                    Settings.Add("pathAssets", pathDirGaius + "\\assets");
                    Log("Pliki Ÿród³owe s¹ gotowe.");
                    if (callback != null) callback();
                });
            });
        }

        public void DownloadNatives(Action callback)
        {
            DownloadFile("http://mcpl.eu/launcher/natives.zip", pathDirGaius + "\\temp\\natives.zip", (sender, e) => { pbarMain.Value = e.ProgressPercentage; }, (sender, e) =>
            {
                pbarMain.Value = 0;
                if (callback != null) callback();
            });
        }

        public void DownloadLast(Action callback)
        {
            DownloadFile("http://mcpl.eu/launcher/_" + Settings["versionCurrent"] + ".zip", pathDirGaius + "\\temp\\temp.zip", (sender, e) => { pbarMain.Value = e.ProgressPercentage; }, (sender, e) =>
            {
                pbarMain.Value = 0;
                if (callback != null) callback();
            });
        }


        private void CopyDir(string SourcePath, string DestinationPath)
        {
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));


            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
        }

        private bool UnzipFile(string zip, string path, ProgressChangedEventHandler progress, RunWorkerCompletedEventHandler completed)
        {
            if (File.Exists(zip) && Directory.Exists(Path.GetDirectoryName(path)))
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = false;

                bw.DoWork += (sender, e) =>
                {
                    string zipToUnpack = zip;
                    string unpackDirectory = path;
                    using (ZipFile zip1 = ZipFile.Read(zipToUnpack))
                    {
                        double counter = 0;
                        double count = zip1.Count;
                        foreach (ZipEntry entry in zip1)
                        {
                            counter++;
                            entry.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                            bw.ReportProgress((int)(counter / count * 100));
                        }
                    }
                };

                bw.ProgressChanged += progress;
                bw.RunWorkerCompleted += completed;
                bw.RunWorkerAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool DownloadFile(string url, string path, DownloadProgressChangedEventHandler progress, AsyncCompletedEventHandler completed)
        {
            if (UrlExists(url) && Directory.Exists(Path.GetDirectoryName(path)))
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(progress);
                webClient.DownloadFileAsync(new Uri(url), path);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool UrlExists(string url)
        {
            HttpWebResponse response = null;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch
            {
                response.Close();
            }
            return false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            tabControl1.Visible = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string nick1 = textBox1.Text.Replace(" ", "");
            if (nick1 != "")
            {
                LaunchMinecraft(nick1, textBox2.Text);
            }
            else
            {
                Helper("Wpisz prawid³owy nick!", true);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings["versionLocal"] = (string)comboBox1.SelectedItem;
        }

        private void Helper(string text, bool error = false)
        {

        }

        private void Log(string text)
        {
            richTextBox1.AppendText(" > " + text + "\n");
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (processFree)
            {
                processFree = false;
                button3.Enabled = false;
                UpdateMinecraft();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (processFree)
            {
                MessageBox.Show("Tymczasowo niedostêpne.");
                /*Log("Proces wgrywania modów.");
                string version = (string)comboBox1.SelectedItem;
                if (version != null)
                {
                    Log("Wgrywam mod do pliku JAR " + version + ".");
                    processFree = false;
                    if (modSelect.ShowDialog() == DialogResult.OK)
                    {
                        using (ZipFile zipjar = ZipFile.Read(pathDirGaius + "\\jar\\" + version + ".jar"))
                        {
                            zipjar.RemoveEntry("META-INF");
                            using (ZipFile zipmod = ZipFile.Read(modSelect.FileName))
                            {
                                foreach (ZipEntry entry in zipmod)
                                {
                                    zipjar.AddEntry(entry.FileName, entry.Source);
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        processFree = true;
                        Log("Anulowano.");
                    }
                }*/
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\screenshots"))
            {
                System.Diagnostics.Process.Start("explorer.exe",Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\screenshots");
            }
        }
    }
    public static class ALE
    {
        public static string DictToString<T, V>(IEnumerable<KeyValuePair<T, V>> items)
        {
            return String.Join(";", items.Select(x => x.Key.ToString() + "=" + x.Value.ToString()).ToArray());
        }
    }
}
