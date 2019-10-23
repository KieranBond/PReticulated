using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

            SaveAsPDFs(saveLocation);

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

        private KeyValuePair<string, List<System.Drawing.Image>> GetPDFToSave()
        {
            KeyValuePair<string, List<System.Drawing.Image>> pdfToSave = new KeyValuePair<string, List<System.Drawing.Image>>();

            if (filePaths == null || filePaths.Count <= 0)
                return pdfToSave;

            string filename = filePaths[0];

            PdfDocument doc = PdfDocument.Load(filename);
            List<System.Drawing.Image> images = new List<System.Drawing.Image>();

            //Get an image for each page
            for (int i = 0; i < doc.PageCount; i++)
            {
                System.Drawing.Image img = doc.Render(i, (int)doc.PageSizes[i].Width, (int)doc.PageSizes[i].Height, 150, 150, PdfRenderFlags.CorrectFromDpi);
                images.Add(img);
            }

            //Keep hold of the images related to each pdf
            if (images != null && images.Count > 0)
            {
                string name = Path.GetFileNameWithoutExtension(filename);
                pdfToSave = new KeyValuePair<string, List<System.Drawing.Image>>(name, images);
            }

            return pdfToSave;
        }

        private void SaveAsPDFs(string saveLocation)
        {
            while(filePaths.Count > 0)
            {
                //Get the next PDF to save
                KeyValuePair<string, List<System.Drawing.Image>> pdf = GetPDFToSave();
                if (string.IsNullOrEmpty(pdf.Key))
                {
                    //Skip this one
                    filePaths.RemoveAt(0);

                    //Make sure we've released everything
                    if (pdf.Value != null && pdf.Value.Count > 0)
                    {
                        for (int x = pdf.Value.Count-1; x >= 0; x--)
                        {
                            pdf.Value[x].Dispose();
                            pdf.Value.RemoveAt(x);
                        }
                    }

                    continue;
                }

                //For each page of the pdf, create a file
                for (int i = pdf.Value.Count-1; i >= 0; i--)
                {
                    string filename = pdf.Key + i.ToString() + ".png";
                    string path = Path.Combine(saveLocation, filename);
                    try
                    {
                        pdf.Value[i].Save(path, ImageFormat.Png);
                    }
                    catch (Exception e)
                    {
                        //Todo: Add handling
                    }
                    finally
                    {
                        pdf.Value[i].Dispose();
                        pdf.Value.RemoveAt(i);
                    }
                }

                filePaths.RemoveAt(0);
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

            Console.WriteLine(location);

            return location;
        }
    }
}
