using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpcUaWinForms {
    public partial class FrmMain : Form {
        private NotifyIcon _trayIcon;

        public FrmMain() {
            InitializeComponent();
            ShowInTaskbar = false;
        }

        protected override void OnLoad(EventArgs e) {
            _trayIcon = new NotifyIcon() {
                Icon = Util.ReadEmbeddedIcon("AppIcon.ico"),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };
            _trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripMenuItem[] {
                new ToolStripMenuItem("Browse", null, Notif_OnBrowse),
                new ToolStripMenuItem("Settings", null, Notif_OnSettings),
                new ToolStripMenuItem("Exit", null, Notif_OnExit)
            });
            base.OnLoad(e);
            WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// Hide from Alt+Tab, and TaskMgr's Applications.
        /// </summary>
        protected override CreateParams CreateParams {
            get {
                var Params = base.CreateParams;
                Params.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW 
                //Params.ExStyle |= 0x00000008; // WS_EX_TOPMOST: top most
                return Params;
            }
        }

        public void OnBrowseSafe() => Invoke(new Action(() => OnBrowse()));
        public void OnBrowse() {
            var diag = new OpenFileDialog();
            if (diag.ShowDialog() == DialogResult.OK) {
                MessageBox.Show("Selection completed.");
            }
            // TODO:
            // If successful:
            // - Fetch subprograms.
            // - Transfer to FTP.
            // - Reset tag.
        }

        // --

        void Notif_OnBrowse(object sender, EventArgs e) => OnBrowse();
        void Notif_OnSettings(object sender, EventArgs e) {}
        void Notif_OnExit(object sender, EventArgs e) { Close(); }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            if (_trayIcon is not null)
                _trayIcon.Visible = false;
        }
    }
}
