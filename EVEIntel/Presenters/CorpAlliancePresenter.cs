using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EVEIntel.Model;
using EVEIntel.Views;

namespace EVEIntel.Presenters
{
    public class CorpAlliancePresenter : PresenterBase<CorpAllianceView>
    {
        private readonly ApplicationPresenter _presenter;
        private readonly PilotGroup _group;

        public CorpAlliancePresenter(ApplicationPresenter presenter, CorpAllianceView view, PilotGroup Group)
            : base(view, "Group.Name")
        {
            View.DataContext = this;
            _presenter = presenter;
            _group = Group;
        }

        public PilotGroup Group
        {
            get { return _group; }
        }

        public bool isCorporation
        {
            get
            {
                return Group is Corporation;
            }
        }

        public bool isAlliance
        {
            get
            {
                return Group is Alliance;
            }
        }

        public void Close()
        {
            _presenter.CloseTab(this);
        }
    }
}
