using GostDOC.Common;
using GostDOC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace GostDOC.ViewModels
{
    class MainWindowVM
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private TreeViewItem _selectedTreeItem = null;
        private DocumentType _selectedDoc = DocumentType.None;
        private DocManager _docManager = DocManager.Instance;

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<SpecificationEntryVM> SpecificationSelectedItem { get; } = new ObservableProperty<SpecificationEntryVM>();
        public ObservableCollection<SpecificationEntryVM> SpecificationTable { get; } = new ObservableCollection<SpecificationEntryVM>();
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<BillEntryVM> BillSelectedItem { get; } = new ObservableProperty<BillEntryVM>();
        public ObservableCollection<BillEntryVM> BillTable { get; } = new ObservableCollection<BillEntryVM>();
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();

        #region Commands
        public ICommand OpenFilesCmd => new Command(OpenFiles);
        public ICommand SaveFileCmd => new Command(SaveFile);
        public ICommand SaveFileAsCmd => new Command(SaveFileAs);
        public ICommand ExitCmd => new Command<Window>(Exit);
        public ICommand AddBillItemCmd => new Command(AddBillItem);
        public ICommand RemoveBillItemsCmd => new Command<IList<object>>(RemoveBillItems);
        public ICommand MergeBillItemsCmd => new Command<IList<object>>(MergeBillItems);
        public ICommand AddSpecificationItemCmd => new Command(AddSpecificationItem);
        public ICommand RemoveSpecificationItemsCmd => new Command<IList<object>>(RemoveSpecificationItems);
        public ICommand MergeSpecificationItemsCmd => new Command<IList<object>>(MergeSpecificationItems);

        public ICommand TreeViewSelectionChangedCmd => new Command<TreeViewItem>(TreeViewSelectionChanged);
        #endregion Commands

        public MainWindowVM()
        {
        }

        #region Commands impl
        private void OpenFiles(object obj)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "All Files *.xml | *.xml";
            open.Multiselect = true;
            open.Title = "Выбрать файлы...";

            if (open.ShowDialog() == DialogResult.OK)
            {
                _docManager.LoadData(open.FileNames);
            }
        }

        private void SaveFile(object obj)
        {
            // TODO: save file / files
        }

        private void SaveFileAs(object obj)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "All Files *.xml | *.xml";
            save.Title = "Сохранить файл";

            if (save.ShowDialog() == DialogResult.OK)
            {
                // TODO: save file / files
            }
        }

        private void TreeViewSelectionChanged(TreeViewItem obj)
        {
            _selectedTreeItem = obj;
            UpdateSelectedDocument();
        }

        private void Exit(Window wnd)
        {
            wnd.Close();
        }
        
        private void AddBillItem(object obj)
        {
            BillTable.Add(new BillEntryVM());
        }
        private void RemoveBillItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<BillEntryVM>().ToList())
            {
                BillTable.Remove(item);
            }
        }
        private void MergeBillItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<BillEntryVM>().ToList())
            {
                // TODO: merge items
                BillTable.Remove(item);                
            }
        }
        private void AddSpecificationItem(object obj)
        {
            SpecificationTable.Add(new SpecificationEntryVM());
        }
        private void RemoveSpecificationItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<SpecificationEntryVM>().ToList())
            {
                SpecificationTable.Remove(item);
            }
        }
        private void MergeSpecificationItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<SpecificationEntryVM>().ToList())
            {
                // TODO: merge items
                SpecificationTable.Remove(item);
            }
        }

        #endregion Commands impl

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
