using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GUI.Model;
using Microsoft.Win32;

namespace GUI.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        public string Title { get; set; }
        private UPKFile _selectedFile;

        public UPKFile SelectedFile {
            get
            {
                return SelectedFile;
            }
            set
            {
                Set(() => SelectedFile, ref _selectedFile, value);
            }
        }

        private List<UPKFile> _uPKFiles;
        public List<UPKFile> UPKFiles { get => _uPKFiles; set { Set(() => UPKFiles, ref _uPKFiles, value); }}

        public RelayCommand<string> ProcessFilesCommand { get; private set; }
        public RelayCommand OpenFilesCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            SetTitle();
            ProcessFilesCommand = new RelayCommand<string>(ProcessFiles, canExecute: (_) => UPKFiles != null && UPKFiles.Count > 0);
            OpenFilesCommand = new RelayCommand(ShowOpenFilesDialog);
        }

        private void ShowOpenFilesDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "UPK files (*.upk)|*.upk";
            if (openFileDialog.ShowDialog() == true)
            {
                UPKFiles = new List<UPKFile>();
                foreach (string filename in openFileDialog.FileNames)
                {
                    try
                    {
                        var upkFile = new UPKFile(filename);
                        UPKFiles.Add(upkFile);
                    }
                    catch (ArgumentException e)
                    {
                        throw new Exception("Not implemented error logging!");
                    }
                }
            }
        }

        private void SetTitle()
        {
            if (IsInDesignMode)
            {
                Title = "UPK Decryptor (Design Mode)";
            }
            else
            {
                Title = "UPK Decryptor";
            }
        }

        private void ProcessFiles(string obj)
        {

        }
    }
}
