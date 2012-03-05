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
using System.Windows.Navigation;
using System.Windows.Shapes;
using EVEIntel.Presenters;

namespace EVEIntel.Views
{
    /// <summary>
    /// Interaction logic for CorpAllianceView.xaml
    /// </summary>
    public partial class CorpAllianceView : UserControl
    {
        public CorpAllianceView()
        {
            InitializeComponent();
        }

        public CorpAlliancePresenter Presenter
        {
            get { return DataContext as CorpAlliancePresenter; }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Presenter.Close();
        }
    }
}
