using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using Microsoft.Win32;

namespace PlayTime
{
    public struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    class KidControl : Form
    {
        public const int INET_TIME = 60 * 30;     //30min
        public const int PLAYER_TIME = 60 * 30;   //30min
        public const int REST_TIME = 60 * 15;     //15min
        public const int TIMER_INTERVAL = 10;     //10 seconds

        private int tInet, tPlayer, tOther, tRest, date;
        public Button button1;
        private Timer time = new Timer();

        [DllImport("Kernel32.dll")]
        public static extern void GetLocalTime(ref SYSTEMTIME pst);
        [DllImport("User32.dll")]
        public static extern Int32 SetFocus(int hWnd);
        [DllImport("User32.dll")]
        public static extern Int32 FindWindow(String lpClassName, String lpWindowName);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Int32 FindWindowEx(int hWndParent, int hWndChildAfter, String lpClassName, String lpWindowName);
        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);
        [DllImport("User32.dll")]
        public static extern int SendMessage(int hWnd, int Msg, int wParam, int lParam);
        [DllImport("User32.dll")]
        public static extern int SendNotifyMessage(int hWnd, int Msg, int wParam, int lParam);
        [DllImport("User32.dll")]
        public static extern Boolean PostMessage(int hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MapVirtualKey(int uCode, int uMapType);

        public void updateReg(int flag)  //flag-0:for read and 1 for write
        {
            RegistryKey regkey;
            string subKey = "Software\\Mousedog\\cat";
            SYSTEMTIME pST = new SYSTEMTIME();

            try
            {
                GetLocalTime(ref pST);
                regkey = Registry.CurrentUser.CreateSubKey(subKey);
                if (flag == 0)
                {
                    date = (int)regkey.GetValue("lastAccessDate", 0);
                    if ((date == 0) //first time run, no registry was set
                      || (date != pST.wDay)) // a new day
                    {
                        tInet = INET_TIME;
                        tPlayer = PLAYER_TIME;
                        tRest = REST_TIME;
                        date = pST.wDay;
                        regkey.SetValue("lastAccessDate", date);
                        regkey.SetValue("inetTimeLeft", tInet);
                        regkey.SetValue("playerTimeLeft", tPlayer);
                        regkey.SetValue("restTimeLeft", tRest);
                    }
                    else
                    {
                        date = (int)regkey.GetValue("lastAccessDate");
                        tInet = (int)regkey.GetValue("inetTimeLeft");
                        tPlayer = (int)regkey.GetValue("playerTimeLeft");
                        tRest = (int)regkey.GetValue("restTimeLeft");
                    }
                }
                else
                {
                    regkey.SetValue("lastAccessDate", date);
                    regkey.SetValue("inetTimeLeft", tInet);
                    regkey.SetValue("playerTimeLeft", tPlayer);
                    regkey.SetValue("restTimeLeft", tRest);
                }
                regkey.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void InitializeMyTimer()
        {
            // Set the interval for the timer.
            time.Interval = 500;
            // Connect the Tick event of the timer to its event handler.
            time.Tick += new EventHandler(timerHandler);
            // Start the timer.
            time.Start();
        }

        private void timerHandler(object sender, EventArgs e)
        {
            int hWnd1 = 0, hWnd2 = 0;

            time.Stop();
            if (this.time.Interval == 500)
            {
                this.Visible = false; //hide the form
                this.time.Interval = 1000 * TIMER_INTERVAL;
            }

            if (tRest <= 0)
            {
                button1.Text = "You'd better to take a rest !";
                this.Visible = true;
                tRest = REST_TIME;
            }
            else tRest -= TIMER_INTERVAL;

            hWnd1 = FindWindow("MozillaUIWindowClass", null);
            hWnd2 = FindWindow("IEFrame", null);
            if ((hWnd1 != 0) || (hWnd2 != 0))
            {
                if (tInet == 60)
                {
                    button1.Text = "Internet Browser would be closed in 1 minute !";
                    this.Visible = true;
                }
                if (tInet <= 0)
                {
                    if (hWnd2 != 0) PostMessage(hWnd2, 0x10, 0, 0);
                    if (hWnd1 != 0)
                    {
                        PostMessage(hWnd1, 0x10, 0, 0);
                        System.Threading.Thread.Sleep(500); //give a little time to wait the confirm dialog popup
                        hWnd1 = FindWindow("MozillaDialogClass", "Quit Firefox");
                        if (hWnd1 != 0) //firefox quit confirm dialog
                        {
                            SetForegroundWindow(hWnd1);
                            keybd_event(0xd, (byte)MapVirtualKey(0xd, 0), 0, 0); //MAPVK_VK_TO_VSC=0 for vk-to-scan_code
                            System.Threading.Thread.Sleep(300); //simulate delay
                            keybd_event(0xd, (byte)MapVirtualKey(0xd, 0), 2, 0); //KEYEVENTF_KEYUP=2
                        }
                    }
                    tInet = 0;
                    button1.Text = "You have run out of time for browsering internet for today";
                    this.Visible = true;
                }
                else tInet -= TIMER_INTERVAL;
            }

            hWnd1 = FindWindow("MediaPlayerClassicW", null);
            hWnd2 = FindWindow("WMPlayerApp", null);
            if ((hWnd1 != 0) || (hWnd2 != 0))
            {
                if (tPlayer == 60)
                {
                    button1.Text = "Movie player would be closed in 1 minute !";
                    this.Visible = true;
                }
                if (tPlayer <= 0)
                {
                    if (hWnd1 != 0) PostMessage(hWnd1, 0x10, 0, 0); //send WM_CLOSE
                    if (hWnd2 != 0) PostMessage(hWnd2, 0x10, 0, 0);
                    button1.Text = "You have run out of time for watching movies for today";
                    this.Visible = true;
                }
                else tPlayer -= TIMER_INTERVAL;
            }

            updateReg(1); //write back to reg
            time.Start();
        }

        public KidControl()
        {
            this.Text = "Be a Good Kid !";
            InitializeMyTimer();
            updateReg(0);

            button1 = new Button();
            button1.Size = new Size(160, 40);
            button1.Location = new Point(10, 10);
            button1.Text = "";
            this.Controls.Add(button1);
            button1.Click += new EventHandler(button1_Click);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new KidControl());
        }
    }
}