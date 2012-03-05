using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using EVEIntel.Dialogs;
using EVEIntel.Presenters;
using EVEIntel.Repository;
using EVEIntel.Model;
using EVEIntel.Util;

namespace EVEIntel
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ApplicationPresenter(this, new DataRepository());
            UpdateBar.DataContext = DataContext;
        }

        public ApplicationPresenter Presenter
        {
            get { return DataContext as ApplicationPresenter; }
        }

        private void SetPlayersOnClipboard_Click(object sender, RoutedEventArgs e)
        {
            // set CurrentPlayers.Names on clipboard
            var items = new StringBuilder();
            foreach(var player in Presenter.CurrentPlayers)
            {
                // TODO: fomat with character id?
                items.AppendLine(player.Name);
            }

            Clipboard.SetText(items.ToString());
        }

        public void AddTab<T>(PresenterBase<T> presenter)
        {
            TabItem newTab = null;
            for (int i = 0; i < tabs.Items.Count; i++)
            {
                TabItem existingTab = (TabItem)tabs.Items[i];
                if (existingTab.DataContext.Equals(presenter))
                {
                    tabs.Items.Remove(existingTab);
                    newTab = existingTab;
                    break;
                }
            }

            if (newTab == null)
            {
                newTab = new TabItem();

                // TODO: Fix!
                Binding headerBinding = new Binding(presenter.TabHeaderPath);
                BindingOperations.SetBinding(newTab, TabItem.HeaderProperty, headerBinding);

                newTab.DataContext = presenter;
                newTab.Content = presenter.View;
            }

            tabs.Items.Insert(0, newTab);
            newTab.Focus();
        }

        public void RemoveTab<T>(PresenterBase<T> presenter)
        {
            for (int i = 0; i < tabs.Items.Count; i++)
            {
                TabItem existingTab = (TabItem)tabs.Items[i];
                if (existingTab.DataContext.Equals(presenter))
                {
                    tabs.Items.Remove(existingTab);
                    break;
                }
            }
        }

        private void ViewPlayer_Click(object sender, RoutedEventArgs e)
        {
            Button button = e.OriginalSource as Button;
            if (button == null)
                return;

            Presenter.ViewPlayer(button.DataContext as Player);
        }

        private void ViewCorp_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentPlayers.SelectedItem == null)
                return;

            var p = CurrentPlayers.SelectedItem as Player;

            if(p==null) return;

            Presenter.ViewCorp(p.Corp);
        }

        private void ViewAlliance_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayers.SelectedItem == null)
                return;

            var p = CurrentPlayers.SelectedItem as Player;

            if (p == null) return;

            Presenter.ViewAlliance(p.Alliance);
        }

    }
}
