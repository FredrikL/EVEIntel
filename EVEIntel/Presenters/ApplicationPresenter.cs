using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EVEIntel.Dialogs;
using EVEIntel.Repository;
using EVEIntel.Model;
using EVEIntel.Repository.Misc;
using EVEIntel.Views;
using System.ComponentModel;

namespace EVEIntel.Presenters
{
    public class ApplicationPresenter : PresenterBase<MainWindow>
    {
        private class WorkerParam
        {
            public string Query { get; set; }
            public QueryTypeEnum Type { get; set; }
            public DateTime MaxAge { get; set; }
        }

        private readonly DataRepository _repository;
        private ObservableCollection<Player> _CurrentPlayers;
        private BackgroundWorker _BackgroundWorker;
        private string _WorkerStatus;

        public ApplicationPresenter(MainWindow view, DataRepository Repository) : base(view)
        {
            _repository = Repository;
            CurrentPlayers = new ObservableCollection<Player>();
            SetupWorker();
        }

        private void SetupWorker()
        {
            _BackgroundWorker = new BackgroundWorker();
            _BackgroundWorker.WorkerReportsProgress = true;
            _BackgroundWorker.WorkerSupportsCancellation = true;

            _BackgroundWorker.DoWork += new DoWorkEventHandler(_BackgroundWorker_DoWork);
            _BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_BackgroundWorker_RunWorkerCompleted);
            _BackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(_BackgroundWorker_ProgressChanged);
        }

        private void _BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            WorkerStatus = e.UserState.ToString();
        }

        private void _BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnPropertyChanged("isWorkerRunning");
            if (e.Cancelled)
                WorkerStatus = "Update Cancelled!";
            else
                WorkerStatus = "Update Complete!";
        }

        private void _BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            OnPropertyChanged("isWorkerRunning");
            BackgroundWorker worker = (BackgroundWorker) sender;
            WorkerParam param = (WorkerParam)e.Argument;
            WorkerStatus = "Update Started!";

            _repository.UpdateData(param.Query, param.Type, param.MaxAge, worker, e);
        }

        public string WorkerStatus
        {
            get { return _WorkerStatus; }
            set
            {
                _WorkerStatus = value;
                OnPropertyChanged("WorkerStatus");
            }
        }

        public bool isWorkerRunning
        {
            get { return _BackgroundWorker.IsBusy; }
        }

        public ObservableCollection<Player> CurrentPlayers
        {
            get { return _CurrentPlayers; }
            set
            {
                _CurrentPlayers = value;
                OnPropertyChanged("CurrentPlayers");
            }
        }

        public void Search(string Query, QueryTypeEnum Type, DateTime FromDate)
        {
            CurrentPlayers = new ObservableCollection<Player>(_repository.GetPilots(Query, Type, FromDate));
            View.tiSearchResults.Focus();
        }

        public void Update(string Query, QueryTypeEnum Type, DateTime MaxAge)
        {
            if(!_BackgroundWorker.IsBusy)
                _BackgroundWorker.RunWorkerAsync(new WorkerParam
                                                 {
                                                     Query = Query,
                                                     Type = Type,
                                                     MaxAge = MaxAge
                                                 });
        }        

        public void CancleUpdate()
        {
            if(_BackgroundWorker.IsBusy)
                _BackgroundWorker.CancelAsync();
        }

        public void ViewPlayer(Player player)
        {
            // update player object with additional data
            player = _repository.GetPlayer(player.Name);

            View.AddTab(new CharacterDetailsPresenter(this, new CharacterDetailsView(), player));
        }
        public void ViewCorp(string CorpName)
        {
            Corporation corp = _repository.GetCorporation(CorpName);
            AddCorpAllianceTab(corp);
        }

        private BackgroundWorker _bwA;
        private LoadingWindow _win;
        public void ViewAlliance(string AllianceName)
        {
            LoadingWindow _win = new LoadingWindow();
            _bwA = new BackgroundWorker();
            _bwA.DoWork += new DoWorkEventHandler(_bwA_DoWork);
            _bwA.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_bwA_RunWorkerCompleted);
            _bwA.RunWorkerAsync(AllianceName);
            _win.ShowWindow();
            //Alliance all = _repository.GetAlliance(AllianceName);
            
            //AddCorpAllianceTab(all);
        }

        void _bwA_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Alliance all = e.Result as Alliance;
            AddCorpAllianceTab(all);
            //_win.CloseWindow();
            //_win = null;
        }

        void _bwA_DoWork(object sender, DoWorkEventArgs e)
        {
            string name = e.Argument as string;
            Alliance all = _repository.GetAlliance(name);
            e.Result = all;
        }

        private void AddCorpAllianceTab(PilotGroup Group)
        {
            View.AddTab(new CorpAlliancePresenter(this, new CorpAllianceView(), Group));
        }

        public void CloseTab<T>(PresenterBase<T> presenter)
        {
            View.RemoveTab(presenter);
        }
    }
}
