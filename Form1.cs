using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

namespace easy2kicadtool
{
    public partial class Form1 : Form
    {
        Process process = new Process();

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public Form1()
        {
            InitializeComponent();

            // 3. Register HotKey

            // Set an unique id to your Hotkey, it will be used to
            // identify which hotkey was pressed in your code to execute something
            int UniqueHotkeyId = 1;
            // Set the Hotkey triggerer the F9 key 
            // Expected an integer value for F9: 0x78, but you can convert the Keys.KEY to its int value
            // See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx
            int HotKeyCode = (int)Keys.E;
            // Register the "CTRL + E" hotkey
            //0x0002 = CTRL, 0x0001 = alt, 0x0004 = shift
            Boolean F9Registered = RegisterHotKey(
                this.Handle, UniqueHotkeyId, 0x0002, HotKeyCode
            );
            
            // 4. Verify if the hotkey was succesfully registered, if not, show message in the console
            if (F9Registered)
            {
                Console.WriteLine("Global Hotkey F9 was succesfully registered");
            }
            else
            {
                Console.WriteLine("Global Hotkey F9 couldn't be registered !");
            }

            try
            {
                if (config.AppSettings.Settings["outputfolder"] != null) textBox3.Text = config.AppSettings.Settings["outputfolder"].Value;
                else textBox3.Text = "INSERT OUTPUT FOLDER";

                if (config.AppSettings.Settings["relativepath"] != null) checkBox1.Checked = Convert.ToBoolean(config.AppSettings.Settings["relativepath"].Value);
                else checkBox1.Checked = true;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }

        }

        protected override void WndProc(ref Message m)
        {
            // 5. Catch when a HotKey is pressed !
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                // MessageBox.Show(string.Format("Hotkey #{0} pressed", id));

                if (id == 1)
                {
                    //MessageBox.Show("F9 Was pressed !");
                    this.Show();
                    this.Focus();
                    this.CenterToScreen();
                    this.BringToFront();
                }
            }

            base.WndProc(ref m);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.CenterToScreen();
            textBox1.Clear();
            textBox1.Focus();
            checkForTool();
        }


        private void checkForTool()
        {

            if (File.Exists("eda2kicad.exe") == true)
            {
                if (new System.IO.FileInfo("eda2kicad.exe").Length < 1000)
                {
                    File.Delete("eda2kicad.exe");
                }
            }

            if (File.Exists("eda2kicad.exe") == false)
            {
                DialogResult dialogResult = MessageBox.Show("There is no eda2kicad.exe found. Would you like to download it now?", "Tool missing", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    //do something
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadFile("https://playground.databyte.ch/services/eda2kicad/eda2kicad.exe", "eda2kicad.exe");
                    }
                }
            }


        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                fetchComponent(textBox1.Text);
               //essageBox.Show("Hello");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                notifyIcon1.Visible = true;
                this.Hide();
                e.Cancel = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Show();
        }


        private void fetchComponent(string lcscID)
        {
            textBox2.AppendText("Started search for: " + lcscID + "...");
            process = new Process();
            process.EnableRaisingEvents = true;
            
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_OutputDataReceived);
            process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(process_ErrorDataReceived);
            process.Exited += new System.EventHandler(process_Exited);

            process.StartInfo.FileName = "eda2kicad.exe";
            process.StartInfo.Arguments = "--full --relative --lcsc_id=" + lcscID + " --output=" + textBox3.Text;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox2.AppendText(value);
         
        }

        void process_Exited(object sender, EventArgs e)
        {
            AppendTextBox(string.Format("process exited with code {0}\r\n", process.ExitCode.ToString()));
        }

        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            AppendTextBox(e.Data + "\r\n");
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            AppendTextBox(e.Data + "\r\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fetchComponent(textBox1.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            if(config.AppSettings.Settings["outputfolder"] == null)
            {
                config.AppSettings.Settings.Add("outputfolder",textBox3.Text);
            }
            else
            {
                config.AppSettings.Settings["outputfolder"].Value = textBox3.Text;
            }

            if (config.AppSettings.Settings["relativepath"] == null)
            {
                config.AppSettings.Settings.Add("relativepath", checkBox1.Checked.ToString());
            }
            else
            {
                config.AppSettings.Settings["relativepath"].Value = checkBox1.Checked.ToString();
            }

            config.Save();
           Application.Exit();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            textBox3.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}
