using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Logger.xaml
    /// </summary>
    public partial class Logger : Window
    {
        public static string name,group;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            name = NickName.Text;
            group = GroupName.Text;
            MainWindow main = new MainWindow();
            main.Show();
            Hide();
            Close();
        }

        public Logger()
        {
            InitializeComponent();
        }
    }
}
