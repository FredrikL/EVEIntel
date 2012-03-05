using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace EVEIntel.Dialogs
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private BackgroundWorker _Worker = null;

        public LoadingWindow()
        {
            InitializeComponent();
            
            _Worker = new BackgroundWorker();
            _Worker.DoWork += new DoWorkEventHandler(_Worker_DoWork);
            _Worker.ProgressChanged += new ProgressChangedEventHandler(_Worker_ProgressChanged);
            _Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_Worker_RunWorkerCompleted);
            _Worker.WorkerReportsProgress = true;
            _Worker.WorkerSupportsCancellation = true;
        }

        void _Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        void _Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress.Value = e.ProgressPercentage;
        }

        void _Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;            
            while (true)
            {
                for (int i = 0; i <= 100; i++)
                    WorkerStuff(i, worker);

                for (int i = 1000; i >= 0; i--)
                    WorkerStuff(i, worker);
            }
        }

        private void WorkerStuff(int status, BackgroundWorker Worker)
        {
            if (Worker.CancellationPending)
                return;
            Worker.ReportProgress(status);
            Thread.Sleep(200);
        }

        public void ShowWindow()
        {
            //_Worker.RunWorkerAsync();
            ShowDialog();
        }

        public void CloseWindow()
        {
            //_Worker.CancelAsync();
            Close();
        }
    }
}
