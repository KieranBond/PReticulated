using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PDFConverter.Services
{
    public class PDFConvertService
    {
        public void SavePDFAsPNG(string filename, string saveFolder)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(saveFolder))
                return;

            using PdfDocument doc = PdfDocument.Load(filename);

            List<Image> images = new List<Image>();

            //Get an image for each page
            for (int i = 0; i < doc.PageCount; i++)
            {
                Image img = doc.Render(i, (int)doc.PageSizes[i].Width, (int)doc.PageSizes[i].Height, 150, 150, PdfRenderFlags.CorrectFromDpi);
                images.Add(img);
            }

            //Keep hold of the images related to each pdf
            if (images != null && images.Count > 0)
            {
                string name = Path.GetFileNameWithoutExtension(filename);
                SavePDF(saveFolder, name, images);
            }
        }


        private bool SavePDF(string saveLocation, string originalFileName, List<Image> pdfImages)
        {
            if (string.IsNullOrEmpty(originalFileName))
            {
                //Make sure we've released everything
                if (pdfImages != null && pdfImages.Count > 0)
                {
                    for (int x = pdfImages.Count - 1; x >= 0; x--)
                    {
                        pdfImages[x].Dispose();
                        pdfImages.RemoveAt(x);
                    }
                }

                return false;
            }

            //For each page of the pdf, create a file
            for (int i = pdfImages.Count - 1; i >= 0; i--)
            {
                string filename = originalFileName + i.ToString() + ".png";
                string path = Path.Combine(saveLocation, filename);
                try
                {
                    pdfImages[i].Save(path, ImageFormat.Png);
                }
                catch (Exception e)
                {
                    //Todo: Add handling
                }
                finally
                {
                    pdfImages[i].Dispose();
                    pdfImages.RemoveAt(i);
                }
            }

            return false;
        }
    }
}
