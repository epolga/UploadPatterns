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
using System.Windows.Shapes;

namespace UploadPatterns
{
    /// <summary>
    /// Interaction logic for NotifierCtl.xaml
    /// </summary>
    public partial class NotifierCtl : UserControl
    {
        public NotifierCtl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void setError(String strMessage)
        {
            SetMessage(strMessage, Colors.Red);
        }

        public void setInfo(String strMessage)
        {
            SetMessage(strMessage, Colors.Blue);
        }

        public void Clear()
        {
            SetMessage("", Colors.Transparent);
        }

        private void SetMessage(String strMessage, Color color)
        {
            Dispatcher.Invoke(new Action(delegate()
            {
                txtMessage.Foreground = new SolidColorBrush(color);
                txtMessage.Text = strMessage;
                brdrMessage.BorderBrush = txtMessage.Foreground;
                Visibility = Visibility.Visible;
            }));
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            Visibility = Visibility.Collapsed;
        }

    }
}
