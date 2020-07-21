using GostDOC.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace GostDOC.ViewModels
{
    class MainWindowVM
    {
        private TreeViewItem _selectedTreeItem = null;
        private DocumentType _selectedDoc = DocumentType.None;

        public ObservableProperty<bool> IsSpecificationTableVisible { get; set; } = new ObservableProperty<bool>(false);
        public ObservableProperty<SpecificationEntryVM> SpecificationSelectedItem { get; set; } = new ObservableProperty<SpecificationEntryVM>();
        public ObservableCollection<SpecificationEntryVM> SpecificationTable { get; set; } = new ObservableCollection<SpecificationEntryVM>();
        public ObservableProperty<bool> IsBillTableVisible { get; set; } = new ObservableProperty<bool>(false);
        public ObservableProperty<BillEntryVM> BillTableSelectedItem { get; set; } = new ObservableProperty<BillEntryVM>();
        public ObservableCollection<BillEntryVM> BillTable { get; set; } = new ObservableCollection<BillEntryVM>();
              

        public ICommand TreeViewSelectionChangedCmd => new Command<TreeViewItem>(TreeViewSelectionChanged);

        public MainWindowVM()
        {
        }
        private void TreeViewSelectionChanged(TreeViewItem obj)
        {
            _selectedTreeItem = obj;
            UpdateSelectedDocument();
        }

        private void UpdateSelectedDocument()
        {
            _selectedDoc = DocumentType.None;
            TreeViewItem item = _selectedTreeItem;
            while (item != null)
            {
                if (item.Header.Equals("Перечень элементов"))
                {
                    _selectedDoc = DocumentType.Elements;
                    break;
                }
                else if (item.Header.Equals("Спецификация"))
                {
                    _selectedDoc = DocumentType.Specification;
                    break;
                }
                else if (item.Header.Equals("Ведомость покупных изделий"))
                {
                    _selectedDoc = DocumentType.Bill;
                    break;
                }
                else if (item.Header.Equals("Ведомость Д27"))
                {
                    _selectedDoc = DocumentType.Bill_D27;
                    break;
                }
                item = item.Parent as TreeViewItem;
            }

            switch (_selectedDoc)
            {
                case DocumentType.Specification:
                    IsSpecificationTableVisible.Value = true;
                    IsBillTableVisible.Value = false;
                    break;
                case DocumentType.Bill:
                    IsSpecificationTableVisible.Value = false;
                    IsBillTableVisible.Value = true;
                    break;
                default:
                    IsSpecificationTableVisible.Value = false;
                    IsBillTableVisible.Value = false;
                    break;
            }
        }
    }
}
