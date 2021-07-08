using System.Windows;
using Microsoft.Win32;

namespace LachisEditor
{
    public partial class NewMainWindow : Window
    {
        public NewMainWindow()
        {
            InitializeComponent();
        }

        void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            var result = ofd.ShowDialog();
            if (result==true)
            {
                MessageBox.Show(ofd.FileName);
            }
        }
    }
}