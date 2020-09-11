using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GostDOC.Common
{
    public static class WebBrowserUtility
    {
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource", typeof(string), typeof(WebBrowserUtility), new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static string GetBindableSource(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject obj, string value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        public static readonly DependencyProperty BindableDataProperty =
            DependencyProperty.RegisterAttached("BindableData", typeof(byte[]), typeof(WebBrowserUtility), new UIPropertyMetadata(null, BindableDataPropertyChanged));

        public static byte[] GetBindableData(DependencyObject obj)
        {
            return (byte[])obj.GetValue(BindableDataProperty);
        }

        public static void SetBindableData(DependencyObject obj, byte[] value)
        {
            obj.SetValue(BindableDataProperty, value);
        }

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser != null)
            {
                string uri = e.NewValue as string;
                if (!string.IsNullOrEmpty(uri))
                {
                    browser.Navigate(new Uri(uri), string.Empty, null, null);
                }
                else
                {                    
                    browser.Source = null;
                }
            }
        }

        public static void BindableDataPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser != null)
            {
                byte[] data = e.NewValue as byte[];
                if (data != null)
                {
                    var http = HttpDataToBrowser.Instance;
                    http.SetData(data);
                    browser.Navigate(new Uri(http.HostUri), string.Empty, null, null);
                }
                else
                {
                    browser.Source = null;
                }
            }
        }
    }
}
