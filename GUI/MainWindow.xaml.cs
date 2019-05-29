using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Multiselect = true;
        //    openFileDialog.Filter = "UPK files (*.upk)|*.upk";
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        foreach (string filename in openFileDialog.FileNames)
        //            lbFiles.Items.Add(Path.GetFileName(filename));
        //    }

        //}
    }
}
