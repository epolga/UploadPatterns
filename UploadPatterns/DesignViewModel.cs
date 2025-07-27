using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UploadPatterns
{
    public class DesignViewModel
    {
          
        public int DesignID { get; set; }

        public string Caption { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string DownloadParams { get; set; }
        public string DesignUrl { get; set; }
        public string Title { get; set; }
        public string TopTipText1 { get; set; }
        public string TopTipText2 { get; set; }
        public string TopTipText3 { get; set; }
        public string TopTipText4 { get; set; }
        public string TopTipText5 { get; set; }
        public string TopTipText6 { get; set; }

        private void Initialize(Design objDesign, char chrSize = 's')
        {
            try
            {
                if (objDesign == null)
                    return;
                DesignID = objDesign.DesignID;
                Caption = objDesign.Caption;
                Description = objDesign.Description;
                Title = string.Format("{0}|{1}", objDesign.Caption, objDesign.DesignID);
                if (objDesign.NColors == 1)
                {
                    string strToReplace = string.Format("{0} colors", objDesign.NColors);
                    string strReplacing = string.Format("{0} color", objDesign.NColors);

                    Description = Description.Replace(strToReplace, strReplacing);
                }
                int nPage = objDesign.NPage.HasValue ? objDesign.NPage.Value : 0;

                ImageUrl = string.Format("{0}-{1}-{2}-Free-Design.jpg", objDesign.Caption.Replace(' ', '-').Replace('\'', '-').Replace('(', '-').Replace(')', '-'), objDesign.DesignID, chrSize);

                FacebookUrl = string.Format("{0}-{1}-{2}-Free-Design-Facebook.jpg", objDesign.Caption.Replace(' ', '-').Replace('\'', '-').Replace('(', '-').Replace(')', '-'), objDesign.DesignID - 1, chrSize);

                DownloadParams = string.Format("DesignID={0}", objDesign.DesignID);

                DesignUrl = string.Format("{0}-{1}-{2}-Free-Design.aspx", objDesign.Caption.Replace(' ', '-').Replace('\'', '-').Replace('(', '-').Replace(')', '-'), objDesign.AlbumID, nPage);
            }
            catch (Exception ex)
            {
            }
        }

        public DesignViewModel(Design objDesign, char chrSize)
        {
            Initialize(objDesign, chrSize);
        }

        public DesignViewModel(Design objDesign)
        {
            Initialize(objDesign, 's');
        }
    }
}
