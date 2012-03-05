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
    /// Interaction logic for UpdateBar.xaml
    /// </summary>
    public partial class UpdateBar : UserControl
    {
        public UpdateBar()
        {
            InitializeComponent();
            //dpAge.SelectedDate = DateTime.Now;
        }

        public ApplicationPresenter Presenter
        {
            get{ return DataContext as ApplicationPresenter;}
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(dpAge.SelectedDate != null)
            Presenter.Update(tbQuery.Text,
                (QueryTypeEnum)Enum.Parse(typeof(QueryTypeEnum), cbQueryType.SelectedValue.ToString()),
                dpAge.SelectedDate ?? DateTime.Now);
            else
            {
                MessageBox.Show("Max age not selected");
                return;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Presenter.CancleUpdate();
        }
    }
}
