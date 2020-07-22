using GostDOC.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GostDOC.UserControls
{
    /// <summary>
    /// Interaction logic for General.xaml
    /// </summary>
    public partial class GraphValues : UserControl
    {
        public GraphValues()
        {
            InitializeComponent();
        }
        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public GraphType GraphType
        {
            get { return (GraphType)GetValue(GraphTypeProperty); }
            set { SetValue(GraphTypeProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(GraphValues), new PropertyMetadata(null));

        public static readonly DependencyProperty GraphTypeProperty =
             DependencyProperty.Register("GraphType", typeof(GraphType), typeof(GraphValues), new PropertyMetadata(null));
    }
}
