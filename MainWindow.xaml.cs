using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PDFConverter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PDFConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string defaultOpenFolder = null;
        private List<string> filePaths = new List<string>();

        private bool _canRemoveFromList = true;

        public MainWindow()
        {
            InitializeComponent();
            filePaths = new List<string>();
            filePaths.Clear();
        }

        private void FileBrowserOpenClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "PDF Files (*.pdf) | *.pdf";
            fileDialog.Multiselect = true;
            fileDialog.RestoreDirectory = true;

            //If no path has been set yet or the path is non-existent we'll default to My Docs
            if (defaultOpenFolder == null || !Directory.Exists(defaultOpenFolder))
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                fileDialog.InitialDirectory = defaultOpenFolder;

            if (fileDialog.ShowDialog() == true)
            {
                //So that we open in the same place next time
                defaultOpenFolder = Path.GetDirectoryName(fileDialog.FileNames[0]);

                foreach (string filename in fileDialog.FileNames)
                {
                    selectedFiles.Items.Add(Path.GetFileName(filename));
                    filePaths.Add(filename);
                }
            }
        }

        private void ConvertButtonClick(object sender, RoutedEventArgs e)
        {
            _canRemoveFromList = false;

            string saveLocation = null;
            if(filePaths != null && filePaths.Count > 0)
                saveLocation = PromptSaveLocation();

            Parallel.ForEach(filePaths, file =>
            {
                PDFConvertService converter = new PDFConvertService();
                converter.SavePDFAsPNG(file, saveLocation);
            });

            selectedFiles.Items.Clear();
            filePaths.Clear();

            _canRemoveFromList = true;
        }

        private void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!_canRemoveFromList)
                return;

            if(selectedFiles.SelectedItem != null || selectedFiles.SelectedItems != null)
            {
                List<string> filenames = new List<string>();
                List<int> indexes = new List<int>();

                //Get selected filenames and indices
                if(selectedFiles.SelectionMode == System.Windows.Controls.SelectionMode.Single)
                {
                    filenames.Add(selectedFiles.SelectedItem.ToString());
                    indexes.Add(selectedFiles.SelectedIndex);
                }
                else
                {
                    foreach(object selected in selectedFiles.SelectedItems)
                    {
                        filenames.Add(selected.ToString());
                        indexes.Add(selectedFiles.Items.IndexOf(selected));
                    }
                }

                //Descending sort.. So we start at the bottom of the selected files list and don't create any null ref's when deleting
                indexes.Sort((a, b) => -1 * a.CompareTo(b));

                //Go through and remove from our filePath list and the display list
                for(int i = 0; i < indexes.Count; i++)
                {
                    //Remove from the displayed list
                    selectedFiles.Items.RemoveAt(indexes[i]);

                    //Remove from our filepath list
                    int index = 0;
                    for(int x = 0; x < filePaths.Count; x++)
                    {
                        if (filePaths[x].Contains(filenames[i]))
                        {
                            index = x;
                            break;
                        }
                    }
                    filePaths.RemoveAt(index);
                }
            }
        }

        private string PromptSaveLocation()
        {
            string location = null;

            CommonOpenFileDialog fileDialog = new CommonOpenFileDialog();
            fileDialog.IsFolderPicker = true;

            //If no path has been set yet or the path is non-existent we'll default to My Docs
            if (defaultOpenFolder == null || !Directory.Exists(defaultOpenFolder))
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                fileDialog.InitialDirectory = defaultOpenFolder;


            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                location = fileDialog.FileName;
            }

            return location;
        }
    }
}
