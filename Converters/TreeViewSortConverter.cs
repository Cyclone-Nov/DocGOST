using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GostDOC.Converters
{
    public class TreeViewSortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection != null)
            {
                ListCollectionView view = new ListCollectionView(collection);
                SortDescription sort = new SortDescription(parameter.ToString(), ListSortDirection.Ascending);
                view.SortDescriptions.Add(sort);
                return view;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
