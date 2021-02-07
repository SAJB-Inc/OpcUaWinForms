using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpcUaWinForms {
    public class DbgApplicationContext : ApplicationContext {
        private NotifyIcon _trayIcon;
        private CancellationTokenSource _cancelTokenSrc;
        private SynchronizationContext _ctx;

        public DbgApplicationContext() {
            _trayIcon = new NotifyIcon() {
                Icon = Util.ReadEmbeddedIcon("AppIcon.ico"),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };
            _trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripMenuItem[] {
                new ToolStripMenuItem("Browse", null, OnBrowse),
                new ToolStripMenuItem("Settings", null, OnSettings),
                new ToolStripMenuItem("Exit", null, OnExit)
            });
            _cancelTokenSrc = new CancellationTokenSource();
            _ctx = SynchronizationContext.Current;
            Task.Run(
                () => OpcSubThread.Run(
                    _cancelTokenSrc.Token,
                    () => _ctx.Post(o => OnBrowse(null, null), null)
                )
            );
        }

        void OnBrowse(object sender, EventArgs e) {
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
        void OnSettings(object sender, EventArgs e) {
            // TODO: 
        }
        void OnExit(object sender, EventArgs e) {
            _cancelTokenSrc?.Cancel();
            _trayIcon?.Dispose();
            //_trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
