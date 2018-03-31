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
						FileSort(config.From, config.To, config.Regex);
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

		private void FileSort(string from, string to, string regex)
		{
			string[] filePaths = Directory.GetFiles(from, "*", SearchOption.TopDirectoryOnly);

			foreach (var filePath in filePaths)
			{
				string file = Path.GetFileName(filePath);

				if (Regex.IsMatch(file, regex))
				{
					File.Move(filePath, to + file);
					logger.Info(FileMoveFormat(filePath, to + file));
				}

			}
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
	}

}
