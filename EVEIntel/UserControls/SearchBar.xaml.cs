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
using EVEIntel.Repository.Misc;

namespace EVEIntel.UserControls
{
    /// <summary>
    /// Interaction logic for SearchBar.xaml
    /// </summary>
    public partial class SearchBar : UserControl
    {
        public SearchBar()
        {
            InitializeComponent();
            dpMaxAge.SelectedDate = DateTime.Now.AddMonths(-1);
        }

        public ApplicationPresenter Presenter
        {
            get
            {
                return DataContext as ApplicationPresenter;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Presenter.Search(tbQuery.Text,
                (QueryTypeEnum)Enum.Parse(typeof(QueryTypeEnum), cbType.SelectedValue.ToString()),
                dpMaxAge.SelectedDate ?? DateTime.Now.AddMonths(-1));
        }


    }
}
