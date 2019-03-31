using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;

namespace ScreenShotSorter
{
    public partial class Form1 : Form
    {
        private ILog logger;

        private List<Config> configs;

        private bool exitFlag = false;

        public Form1()
        {
            InitializeComponent();

            Hide();

            logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            logger.Info("ScreenShotSorterを起動しました");

            var interval = Properties.Settings.Default.Interval;
            var configFile = Properties.Settings.Default.ConfigFile;

            exitFlag = true;
            try
            {
                timer1.Interval = Convert.ToInt32(interval);

                ConfigLoad(configFile);

                logger.Info("すべての設定を読み込みました");

                timer1.Enabled = true;

                exitFlag = false;
            }
            catch (FileNotFoundException)
            {
                logger.Error("[" + configFile + "]が存在しません");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }

        private void ConfigLoad(string configFile)
        {
            try
            {
                var jsonSerializer = new JsonSerializer();

                using (var reader = new JsonTextReader(new StreamReader(configFile, Encoding.UTF8)))
                {
                    configs = jsonSerializer.Deserialize<List<Config>>(reader);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }

        private void EndToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (exitFlag) Application.Exit();
            else
            {
                foreach (var config in configs)
                {
                    try
                    {
                        FileSort(config);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.ToString());
                        exitFlag = true;
                    }
                }
            }

        }

        private string FileMoveFormat(string from, string to)
        {
            return "ファイル移動 [" + from + "] -> [" + to + "]";
        }

        private void FileSort(Config config)
        {
            string[] filePaths = Directory.GetFiles(config.From, "*", SearchOption.TopDirectoryOnly);

            foreach (var filePath in filePaths)
            {
                string file = Path.GetFileName(filePath);

                if (Regex.IsMatch(file, config.Regex))
                {
                    string toFileName = FileRename(file, config.Rename);

                    try
                    {
                        File.Move(filePath, config.To + toFileName);
                        logger.Info(FileMoveFormat(filePath, config.To + file));
                    }
                    catch (IOException)
                    {
                        logger.Warn(FileMoveFormat(filePath, config.To + file));
                    }


                }

            }
        }

        private string FileRename(string file, string rename)
        {
            if (string.IsNullOrEmpty(rename))
                return file;

            DateTime now = DateTime.Now;

            Regex reg = new Regex("<time>(?<time>.*?)</time>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Match match = reg.Match(rename);

            if (match.Success)
            {
                string format = match.Groups["time"].Value;

                int index = match.Index;

                string toFileName = rename.Remove(index, match.Length);
                toFileName = toFileName.Insert(index, now.ToString(format));

                return toFileName;
            }

            return file;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            logger.Info("ScreenShotSorterを終了しました");
        }

        private void ReloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigLoad(Properties.Settings.Default.ConfigFile);
            logger.Info("ソート設定を読み込みました");
        }
    }

    public class Config
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Regex { get; set; }
        public string Rename { get; set; }
    }

}
