using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.Model
{
    public class UPKFile : ObservableObject
    {

        public string FilePath { get; private set; }
        public string FileName { get { return Path.GetFileNameWithoutExtension(FilePath); } }

        public UPKFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"The path {filePath} does not exist.");
            }
            if (!filePath.ToLower().EndsWith(".upk"))
            {
                throw new ArgumentException($"The path {filePath} is not a upk file.");
            }
            FilePath = filePath;
            
        }
    }
}
