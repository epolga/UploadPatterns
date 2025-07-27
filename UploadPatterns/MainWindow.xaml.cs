using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.Configuration;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
namespace UploadPatterns
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        readonly string m_strEmailID = DateTime.Now.ToString("yyMMdd");
        GetBatchCtl[] getBatchCtls;
        TabItem[] tabItems;
        const int cm_nDesigns = 1;//0;//
        const int cm_nDesignsInNewsLetter = 1;//0;//
        public MainWindow()
        {
            InitializeComponent();
            Console.Beep(200, 600);
            Console.Beep((int)400, 600);
            Console.Beep((int)100, 1200);
            Console.Beep((int)523.2, 300);
            m_notifyIcon = new NotifyIcon();
            m_notifyIcon.BalloonTipText = "Newsletter Sending app has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipTitle = "Newsletter Sending";
            m_notifyIcon.Text = "Newsletter Sending";
           
            getBatchCtls = new GetBatchCtl[cm_nDesigns];
            getBatchCtls[0] = ctlGetBatch1;//
            //getBatchCtls[1] = ctlGetBatch2;
            //getBatchCtls[2] = ctlGetBatch3;
            tabItems = new TabItem[cm_nDesigns];
            tabItems[0] = tabItem1;//
            //tabItems[1] = tabItem2;
            //tabItems[2] = tabItem3;
 
            ctlNotifier.Visibility = Visibility.Visible;
            ctlNotifier.BringIntoView();
        }
        static HttpClient client = new HttpClient();

        void ResizePhotos(int designId)
        {
            try
            {
                var Result = client.PostAsJsonAsync<string>("http://localhost:56311/api/Albums/ResizePhoto?designId=34", designId.ToString())
                    .Result;
                var res = Result.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {

            }
        }

        void Uploader_UploadCompleted()
        {
            ctlNotifier.setInfo("Done");
        }

        void Uploader_UploadError(string strError)
        {
            ctlNotifier.setError(strError);
        }

        void Uploader_UploadProgress(long lngBytesUploaded, long lngFileSize)
        {
            int iPerc = (int)((lngBytesUploaded * 100) / lngFileSize);
            ctlNotifier.setInfo(string.Format("{0}/{1} {2}%",
                    lngBytesUploaded, lngFileSize, iPerc));
        }

      
     

        bool EverythingReady()
        {
            List<GetBatchCtl> lstBatches = GetBatchCtl.Designs;
            for (int i = 0; i < cm_nDesigns; i++)
            {
                GetBatchCtl batch = lstBatches[i];
                if (!batch.Ready)
                    return false;
            }
            return true;
        }

        bool SomethingStarted()
        {
            List<GetBatchCtl> lstBatches = GetBatchCtl.Designs;
            foreach (GetBatchCtl batch in lstBatches)
            {
                if (batch.Started)
                    return true;
            }
            return false;
        }

       


        void OnClose(object sender, CancelEventArgs args)
        {
            if (SomethingStarted() && !EverythingReady())
            {
                brdrAskToClose.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
        }

        private WindowState m_storedWindowState = WindowState.Normal;
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                if (m_notifyIcon != null)
                {
                    m_notifyIcon.Visible = true;
                    m_notifyIcon.ShowBalloonTip(500);
                }
            }
            else
                m_storedWindowState = WindowState;
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

        void Clean()
        {
            if (m_notifyIcon != null)
            {
                m_notifyIcon.Dispose();
                m_notifyIcon = null;
            }
        }

        ~MainWindow()
        {
            Clean();
        }

        void IDisposable.Dispose()
        {
            Clean();
            GC.SuppressFinalize(true);
        }

        
       
 
        void UpdateLocalSitemap(List<Design> lstDesigns)
        {
            if (lstDesigns.Count < 3)
            {
                System.Windows.MessageBox.Show(string.Format("Not enought designs: {0}", lstDesigns.Count));
                return;
            }
            foreach (var design in lstDesigns)
            {
                UpdateMainSitemap(design);
                UpdateAlbumSitemap(design);
            }
        }

        public void UpdateAlbumSitemap(Design design)
        {
            try
            {
                string strSitePath = ConfigurationManager.AppSettings["LocalSitePath"];
                string strSceme = ConfigurationManager.AppSettings["SiteScheme"];
                string strSiteDomain = string.Format("{0}://{1}", strSceme, ConfigurationManager.AppSettings["SiteDomain"]);
                string strFtpRoot = ConfigurationManager.AppSettings["RootFTPDirectory"];
                string strPath = System.IO.Path.Combine(strFtpRoot, "Sitemaps");
                string strSitemapName = string.Format("Sitemap{0:0000}.xml", design.AlbumID);
                string strSitemapPath = System.IO.Path.Combine(strPath, strSitemapName);

                FileInfo fileInfo = new FileInfo(strSitemapPath);
                if (!fileInfo.Exists)
                {
                    Utils.Logger.ErrorFormat("Sitemap {0} doesn't exist", strSitemapPath);
                }
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(strSitemapPath);
                XmlElement childUrlSet = (XmlElement)xmlDocument.SelectSingleNode("/urlset");
                if (childUrlSet != null)
                {
                    var firstElem = childUrlSet.FirstChild;

                    foreach (XmlNode child in firstElem.ChildNodes)
                    {
                        if (child.Name == "lastmod")
                        {
                            child.InnerText = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                    }
                    XmlElement urlElem = xmlDocument.CreateElement("url"); //item1 ,item2..
                    childUrlSet.AppendChild(urlElem);
                    int nPage = design.NPage.HasValue ? design.NPage.Value : 0;
                    int nAlbum = design.AlbumID;
                    XmlElement locElem = xmlDocument.CreateElement("loc"); //item1 ,item2..
                    locElem.InnerText = string.Format("{0}/{1}-{2}-{3}-Free-Design.aspx", strSiteDomain, design.Caption.Replace(" ", "-"), nAlbum, nPage);
                    urlElem.AppendChild(locElem);
                    XmlElement priorityElem = xmlDocument.CreateElement("priority"); //item1 ,item2..
                    priorityElem.InnerText = "1.0";
                    urlElem.AppendChild(priorityElem);
                    XmlElement lastmodElem = xmlDocument.CreateElement("lastmod"); //item1 ,item2..
                    lastmodElem.InnerText = DateTime.Now.ToString("yyyy-MM-dd");
                    urlElem.AppendChild(lastmodElem);
                    childUrlSet.InsertAfter(urlElem, childUrlSet.FirstChild);
                }
                xmlDocument.Save(strSitemapPath);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("In CreateDesignsList", ex);
            }
        }

        public void UpdateMainSitemap(Design design)
        {
            try
            {
                string strSitePath = ConfigurationManager.AppSettings["LocalSitePath"];
                string mainSitemapPath = System.IO.Path.Combine(strSitePath, "Sitemap.xml");
                FileInfo fileInfo = new FileInfo(mainSitemapPath);
                if (fileInfo.IsReadOnly)
                {
                    System.Windows.MessageBox.Show(string.Format("File {0} is readonly", mainSitemapPath));
                    return;
                }
                XmlDocument xmlMainSitemapDocument = new XmlDocument();
                xmlMainSitemapDocument.Load(mainSitemapPath);
                XmlNamespaceManager manager = new XmlNamespaceManager(xmlMainSitemapDocument.NameTable);
                manager.AddNamespace("s", "http://www.sitemaps.org/schemas/sitemap/0.9");
                var sitemapNode = (XmlElement)xmlMainSitemapDocument.SelectSingleNode("/sitemapindex");
                XmlNodeList sitemapNodes = xmlMainSitemapDocument.SelectNodes("/s:sitemapindex/s:sitemap", manager);
                foreach (var node in sitemapNodes)
                {
                    XmlElement xmlElement = node as XmlElement;
                    var locNode = xmlElement.SelectSingleNode("s:loc", manager);
                    string strSitemapName = string.Format("sitemap{0:0000}", design.AlbumID);
                    if (locNode.InnerText.Contains(strSitemapName))
                    {
                        var lastmodNode = xmlElement.SelectSingleNode("s:lastmod", manager);
                        lastmodNode.InnerText = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                }
                xmlMainSitemapDocument.Save(mainSitemapPath);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("In UpdateMainSitemap", ex);
            }
        } 
        
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ButtonCancelExit_Click(object sender, RoutedEventArgs e)
        {
            brdrAskToClose.Visibility = Visibility.Collapsed;
        }

        
        void ShowResult(String strFilename)
        {
            if (!File.Exists(strFilename))
            {
                System.Windows.MessageBox.Show(String.Format("File {0} doesn't exist", strFilename));
                return;
            }

            Process.Start(strFilename);
        }

        string GetRandomAlbums(int nAlbums)
        {

            string strResult = string.Empty;
            return strResult;
        }


        static int RandNumber(int Low, int High)
        {
            Random rndNum = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));

            int rnd = rndNum.Next(Low, High);

            return rnd;
        }
 
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            brdrBody.Visibility = Visibility.Collapsed;
        }
 

        string CheckFirstLevelDirectory(DirectoryInfo directoryInfo)
        {
            string strResult = string.Empty; ;

            FileInfo[] fileInfos = directoryInfo.GetFiles();
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            int nDirectories = cm_nDesigns;

            if (fileInfos.Length > 0)
                strResult += string.Format("\tThere are redundant files\n");

            if (directoryInfos.Length != nDirectories)
                strResult += string.Format("\tThe number of directories is not equal to {0}\n", nDirectories);

            for (int nStep = 0; nStep < nDirectories; nStep++)
            {
                if (nStep >= directoryInfos.Length)
                    break;
                DirectoryInfo nextLevelDirectory = directoryInfos[nStep];

                string strNextLevelResult = CheckSecondLevelDirectory(nextLevelDirectory);
                if (strNextLevelResult != "OK")
                    strResult += string.Format("\n\tErrors in {0} \n{1}\n", nextLevelDirectory.FullName, strNextLevelResult);
            }

            if (string.IsNullOrEmpty(strResult))
                strResult = "OK";
            else
                strResult = string.Format("Errors in {0}:\n{1}", directoryInfo.FullName, strResult);

            return strResult;
        }

        string CheckSecondLevelDirectory(DirectoryInfo directoryInfo)
        {
            string strResult = string.Empty;

            FileInfo[] fileInfos = directoryInfo.GetFiles();

            if (fileInfos.Length != 5)
                strResult += "\t\tThere should be exactly 5 files\n";

            FileInfo[] fileInfosScc = directoryInfo.GetFiles("*.scc");

            if (fileInfosScc.Length != 1)
                strResult += "\t\tThere should be exactly 1 'scc' file\n";

            FileInfo[] fileInfosPdfs = directoryInfo.GetFiles("*.pdf");

            if (fileInfosPdfs.Length != 3)
                strResult += "\t\tThere should be exactly 3 'pdf' files\n";

            FileInfo[] fileInfosTxt = directoryInfo.GetFiles("*.txt");

            if (fileInfosTxt.Length != 1)
                strResult += "\t\tThere should be exactly 1 'txt' file\n";

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            if (directoryInfos.Length > 0)
                strResult += string.Format("\t\tThere are redundant directories in directory\n");

            if (string.IsNullOrEmpty(strResult))
                strResult = "OK";

            return strResult;
        }

        string Verify(string strDirectory)
        {
            string strResult = string.Empty;
            DirectoryInfo directoryInfo = new DirectoryInfo(strDirectory);

            strResult = CheckFirstLevelDirectory(directoryInfo);

            if (string.IsNullOrEmpty(strResult))
                strResult = "OK";

            return strResult;
        }

        string GetRootFolder()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = Properties.Settings.Default.BatchesRoot;
            folderDialog.ShowDialog();
            if (string.IsNullOrEmpty(folderDialog.SelectedPath))
            {
                return string.Empty;
            }

            Properties.Settings.Default.BatchesRoot = folderDialog.SelectedPath;
            Properties.Settings.Default.Save();

            return folderDialog.SelectedPath;
        }

        private void ButtonSetRoot_Click(object sender, RoutedEventArgs e)
        {
            string strRootFolder = GetRootFolder();
            if (string.IsNullOrEmpty(strRootFolder))
                return;
            if (!Directory.Exists(strRootFolder))
            {
                System.Windows.MessageBox.Show(String.Format("Directory {0} doesn't exist", strRootFolder));
                return;
            }
            txtRoot.Text = strRootFolder;
            Properties.Settings.Default.BatchesRoot = strRootFolder;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Utils.Logger.Info("Window_Loaded");
            ButtonSetRoot.Focus();
            ctlNotifier.Visibility = Visibility.Collapsed;
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

        private void btnFromTo_Checked(object sender, RoutedEventArgs e)
        {
            pnlFromTo.Visibility = Visibility.Visible;
        }

        private void btnFromTo_Unchecked(object sender, RoutedEventArgs e)
        {
            pnlFromTo.Visibility = Visibility.Hidden;
        }

       
      
 
        private void ButtonGetImage_Click(object sender, RoutedEventArgs e)
        {
            string strImageFile = GetImageFile();
            StartUploading(strImageFile);
        }

        private string GetImageFile()
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();

            fileDialog.InitialDirectory = Properties.Settings.Default.ImageFile;
            fileDialog.Filter = "jpg files (*.jpg)|*.jpg";//filter
            if (fileDialog.ShowDialog().ToString() == "OK")
            {
                Properties.Settings.Default.ImageFile = fileDialog.FileName;
                Properties.Settings.Default.Save();
            }
            string strFileName = fileDialog.FileName;
            if (string.IsNullOrEmpty(strFileName))
                return string.Empty;
            return strFileName;
        }

        public void StartUploading(string strFileToUpload)
        {
            if (string.IsNullOrEmpty(strFileToUpload))
            {
                Utils.Logger.Debug("StartUploading Image strFolder is empty");
                return;
            }
            Utils.Logger.DebugFormat("StartUploading Image from {0}", strFileToUpload);

            txtImagePath.Text = strFileToUpload;

            string strFileToUploadCopy = strFileToUpload.Substring(0, strFileToUpload.Length - 4); //without ".jpg"
            strFileToUploadCopy += "Copy.jpg";
            File.Copy(strFileToUpload, strFileToUploadCopy, true);

            //DoUploadingImage();
        }

        private void tabSending_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonCreateEmail_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonUploadTemplate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ManualUpload_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonFill_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {

        }
        private void butReset_Click(object sender, RoutedEventArgs e)
        {

        }
        private void butRefresh_Click(object sender, RoutedEventArgs e)
        {


        }
        private void butPause_Click(object sender, RoutedEventArgs e)
        {

        }

        private void butIdle_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {

        }
        private void butClear_Click(object sender, RoutedEventArgs e)
        {

        }




    }
}
