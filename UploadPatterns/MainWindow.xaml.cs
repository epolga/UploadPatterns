using iTextSharp.text.pdf.codec;
using System.Drawing;
using System.IO;
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

namespace UploadPatterns;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        string strPdfPath = txtPdfPath.Text;
        GetImage(strPdfPath);
    }

    void GetImage(String strPDFFileName)
    {
        if (!File.Exists(strPDFFileName))
        {
            ShowMessage(String.Format("No file {0}", strPDFFileName));
            return;
        }

        List<System.Drawing.Image> lstImages = ExtractImages(strPDFFileName);
        if (lstImages.Count < 1)
        {
            ShowMessage("Failed to get Image");
            return;
        }
        Bitmap bitmp = new Bitmap(lstImages[0]);
        bitmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        ImageSource imageSource = ToBitmapSource(bitmp);
        imgBatch.Source = imageSource;
        // BitmapImage image = new BitmapImage(new Uri(m_strImageFileName));
        // imgBatch.Source = image;
        try
        {
          //  bitmp.Save(m_strImageFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        catch
        {
            ShowMessage("Could not save image file");
        }
    }
    private static List<System.Drawing.Image> ExtractImages(String PDFSourcePath)
    {
        List<System.Drawing.Image> ImgList = new List<System.Drawing.Image>();

        iTextSharp.text.pdf.RandomAccessFileOrArray RAFObj = null;
        iTextSharp.text.pdf.PdfReader PDFReaderObj = null;
        iTextSharp.text.pdf.PdfObject PDFObj = null;
        iTextSharp.text.pdf.PdfStream PDFStremObj = null;

        try
        {
            RAFObj = new iTextSharp.text.pdf.RandomAccessFileOrArray(PDFSourcePath);
            PDFReaderObj = new iTextSharp.text.pdf.PdfReader(RAFObj, null);

            for (int i = 0; i <= PDFReaderObj.XrefSize - 1; i++)
            {
                PDFObj = PDFReaderObj.GetPdfObject(i);

                if ((PDFObj != null) && PDFObj.IsStream())
                {
                    PDFStremObj = (iTextSharp.text.pdf.PdfStream)PDFObj;
                    iTextSharp.text.pdf.PdfObject subtype = PDFStremObj.Get(iTextSharp.text.pdf.PdfName.SUBTYPE);

                    if ((subtype != null) && subtype.ToString() == iTextSharp.text.pdf.PdfName.IMAGE.ToString())
                    {
                        try
                        {

                            iTextSharp.text.pdf.parser.PdfImageObject PdfImageObj =
                     new iTextSharp.text.pdf.parser.PdfImageObject((iTextSharp.text.pdf.PRStream)PDFStremObj);

                            System.Drawing.Image ImgPDF = PdfImageObj.GetDrawingImage();


                            ImgList.Add(ImgPDF);

                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            PDFReaderObj.Close();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        return ImgList;
    }
    void ShowMessage(String strMessage)
    {
        if (strMessage == null)
            strMessage = String.Empty;
        Dispatcher.Invoke(new Action(delegate ()
        {
            txtMessage.Text = strMessage;
            brdrMessage.Visibility = Visibility.Visible;
        }));
    }

    public static BitmapSource ToBitmapSource(System.Drawing.Bitmap source)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            source.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

            stream.Position = 0;
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
            // Force the bitmap to load right now so we can dispose the stream.
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.StreamSource = stream;
            result.EndInit();
            result.Freeze();
            return result;
        }
    }
}

