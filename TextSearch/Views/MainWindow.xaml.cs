using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextSearch.Helpers;
using TextSearch.ViewModels;

namespace TextSearch.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).StopSearch();
        }

        private void PauseResumeButtonClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).PauseResumeSearch();
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).StartSearch(StartUrlTextBox.Text, (int)MaxThreadUpDown.Value, SearchTextTextBox.Text, (int)MaxUrlUpDown.Value);
        }
    }
}
