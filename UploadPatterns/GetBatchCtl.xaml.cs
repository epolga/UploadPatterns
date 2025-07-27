using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Configuration;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Threading.Tasks;
 

namespace UploadPatterns
{
    /// <summary>
    /// Interaction logic for GetBatchCtl.xaml
    /// </summary>ButtonUpload_Click
    public partial class GetBatchCtl : System.Windows.Controls.UserControl
    {
        static List<GetBatchCtl> m_slstDesigns = new List<GetBatchCtl>();

        public static List<GetBatchCtl> Designs
        {
            get 
            {
                return m_slstDesigns;
            }
        }

        static AutoResetEvent m_eventAddedToDB = new AutoResetEvent(true);
        public delegate void ImageUploadedDelegate(GetBatchCtl sender);
        public event ImageUploadedDelegate ImageUploaded;
        public delegate void EverythingUploadedDelegate(int uploadingID);
        public event EverythingUploadedDelegate EverythingUploaded;
     
        public static readonly DependencyProperty UploadingIDProperty = DependencyProperty.Register("UploadingID", typeof(int), typeof(GetBatchCtl));
        public int UploadingID
        {
            get { return (int)this.GetValue(UploadingIDProperty); }
            set { this.SetValue(UploadingIDProperty, value); }
        }

        bool m_bImageReady = true;
        public bool ImageReady
        {
            get
            {
             lock (this)
                {
                    return m_bImageReady;
                }
            }
            set
            {
              //  lock (this)
                {
                    m_bImageReady = value;
                }
            }
        }  
        
        bool m_bReady = false;
        public bool Ready
        {
            get
            {
                //lock (this)
                {
                    return m_bReady;
                }
            }
            set
            {
                lock (this)
                {
                    m_bReady = value;
                }
            }
        }

        bool m_bStarted = false;
        public bool Started
        {
            get
            {
                // lock (this)
                {
                    return m_bStarted;
                }
            }
            set
            {
                lock (this)
                {
                    m_bStarted = value;
                }
            }
        }

        public int DesignID
        {
            get
            {
                if (m_patternInfo == null)
                    return -1;
                return m_patternInfo.DesignID;
            }

        }

        public string DesignTitle
        {
            get
            {
                if (m_patternInfo == null)
                    return string.Empty;
                return m_patternInfo.Title;
            }

        }

        string m_strImageFileName = string.Empty;
        string m_strBatchFolder = string.Empty;
        PatternInfo m_patternInfo = null;
        int m_iAlbumID = -1;

        string m_strFTPHost = string.Empty;

        public string FTPHost
        {
            get { return m_strFTPHost; }
            set { m_strFTPHost = value; }
        }
        string m_strFTPUsername = string.Empty;

        public string FTPUsername
        {
            get { return m_strFTPUsername; }
            set { m_strFTPUsername = value; }
        }
        string m_strFTPPassword = string.Empty;

        public string FTPPassword
        {
            get { return m_strFTPPassword; }
            set { m_strFTPPassword = value; }
        }

        public GetBatchCtl()
        {
            InitializeComponent();
            GetFTPSettings();
            m_slstDesigns.Add(this);
        }


        void GetFTPSettings()
        {
            FTPHost = ConfigurationManager.AppSettings["FTPHost"];
            FTPUsername = ConfigurationManager.AppSettings["FTPUser"];
            FTPPassword = ConfigurationManager.AppSettings["FTPPassword"];
        }

        public void StartUploading(string strFolder)
        {
            if (string.IsNullOrEmpty(strFolder))
            {
                Utils.Logger.Debug("StartUploading  strFolder is empty");
                return;
            }
            Utils.Logger.DebugFormat("StartUploading  from {0}", strFolder);
            m_strBatchFolder = strFolder;

            txtBatchPath.Text = m_strBatchFolder;
            GetBatch();
            DoUploading();
        }

        private void ButtonGetBatch_Click(object sender, RoutedEventArgs e)
        {
            m_strBatchFolder = GetFolderName();
            StartUploading(m_strBatchFolder);
        }

        private string GetFolderName()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            folderDialog.InitialDirectory = Properties.Settings.Default.BatchesRoot;
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.BatchesRoot = folderDialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
            string strFolderName = folderDialog.SelectedPath;
            if (string.IsNullOrEmpty(strFolderName))
                return string.Empty;
            return strFolderName;
        }

        void GetBatch()
        {
            if (!Directory.Exists(m_strBatchFolder))
            {
                ShowMessage(String.Format("Directory {0} doesn't exist", m_strBatchFolder));
                return;
            }
            Utils.Logger.DebugFormat("Starting GetBatch m_strBatchFolder = {0}", m_strBatchFolder);
            string[] txtAlbums = Directory.GetFiles(m_strBatchFolder, "*.txt");
            string txtAlbum = txtNAlbum.Text + ".txt";
            if (txtAlbums.Length == 1)
            {
                //ShowMessage("There should be one and only one 'txt' file inside the folder");
                //return;
                txtAlbum = txtAlbums[0];
            }


            string strAlbum = Path.GetFileNameWithoutExtension(txtAlbum);
            if (!Int32.TryParse(strAlbum, out m_iAlbumID))
            {
                m_iAlbumID = -1;
                ShowMessage("Cannot get album number");
                return;
            }
            else
                txtNAlbum.Text = strAlbum;

            Utils.Logger.DebugFormat("Starting GetBatch strAlbum = {0}", strAlbum);

            string strPDFFileName = Path.Combine(m_strBatchFolder, "1.pdf");

            GetPDF(strPDFFileName);

            Utils.Logger.DebugFormat("After GetPDF strPDFFileName = {0}", strPDFFileName);

            brdrMessage.Visibility = Visibility.Hidden;
            m_strImageFileName = Path.Combine(m_strBatchFolder, "1.jpg");// arrstrFiles[0];

            GetImage(strPDFFileName);

            Utils.Logger.DebugFormat("After GetImage strPDFFileName = {0}", strPDFFileName);

            txtNAlbum.Focus();
        }

        void GetPDF(string strPDFFile)
        {
            m_patternInfo = new PatternInfo(strPDFFile);
            txtTitle.Text = m_patternInfo.Title;
            txtNotes.Text = m_patternInfo.Notes;
            txtWidth.Text = m_patternInfo.Width.ToString();
            txtHeight.Text = m_patternInfo.Height.ToString();
            txtNColors.Text = m_patternInfo.NColors.ToString();
        }

        void GetImage()
        {
            if (!File.Exists(m_strImageFileName))
            {
                ShowMessage(String.Format("No file at {0}", m_strImageFileName));
                return;
            }

            BitmapImage image = new BitmapImage(new Uri(m_strImageFileName));
            imgBatch.Source = image;
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
            try {
                bitmp.Save(m_strImageFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            catch 
            {
                ShowMessage("Could not save image file");
            }
        }
        
        
        void ShowMessage(String strMessage)
        {
            if (strMessage == null)
                strMessage = String.Empty;
            Dispatcher.Invoke(new Action(delegate()
            {
                txtMessage.Text = strMessage;
                brdrMessage.Visibility = Visibility.Visible;
            }));
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            brdrMessage.Visibility = Visibility.Hidden;
        }

        private void DoUploading()
        {
            ThreadStart ts = new ThreadStart(UploadDesign);
            Thread th = new Thread(ts);
            th.Start();
        }
        
        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            if (m_iAlbumID < 0)
            {
                if (!Int32.TryParse(txtNAlbum.Text, out m_iAlbumID))
                {
                    ShowMessage("Wrong Album Number");
                    m_iAlbumID = -1;
                    return;
                }
            }

            DoUploading();
        }
   

        void UploadDesign()
        {
         
           }

        
       
        TextBlock GetTextBlockToUpdate(int nPDF)
        {
            TextBlock txtBlockToUpdate = null;
            switch (nPDF)
            {
                case 0:
                    txtBlockToUpdate = txtPDF0Progress;
                    break;
                case 1:
                    txtBlockToUpdate = txtPDF1Progress;
                    break;
                case 2:
                    txtBlockToUpdate = txtPDF2Progress;
                    break;
                case 3:
                    txtBlockToUpdate = txtPDF3Progress;
                    break;
                case 4:
                    txtBlockToUpdate = txtPDF4Progress;
                    break;
                case 5:
                    txtBlockToUpdate = txtPDF5Progress;
                    break;
                case 6:
                    txtBlockToUpdate = txtPDF6Progress;
                    break;
                default:
                    return null;
            }
            return txtBlockToUpdate;
        }

       
         
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtBatchPath.Text))
                btnGetBatch.Focus();
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

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {

            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}
