using GostDOC.Common;
using GostDOC.Models;
using GostDOC.Views;
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
        private DocManager _docManager = DocManager.Instance;

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<SpecificationEntry> SpecificationSelectedItem { get; } = new ObservableProperty<SpecificationEntry>();
        public ObservableCollection<SpecificationEntry> SpecificationTable { get; } = new ObservableCollection<SpecificationEntry>();
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<BillEntry> BillSelectedItem { get; } = new ObservableProperty<BillEntry>();
        public ObservableCollection<BillEntry> BillTable { get; } = new ObservableCollection<BillEntry>();
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValues> GeneralGraphValues { get; } = new ObservableCollection<GraphValues>();
        public ObservableCollection<TreeViewItem> SpecificationGroups { get; } = new ObservableCollection<TreeViewItem>();
        public ObservableCollection<TreeViewItem> BillGroups { get; } = new ObservableCollection<TreeViewItem>();

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
        public ICommand SaveGraphValuesCmd => new Command<GraphType>(SaveGraphValues);

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
                string mainFileName = null;
                if (open.FileNames.Length > 1)
                {
                    mainFileName = GetMainFileName(open.SafeFileNames);
                    if (string.IsNullOrEmpty(mainFileName))
                        return;
                }
                // Parse xml files
                _docManager.XmlManager.LoadData(open.FileNames, mainFileName);
                // Update visual data
                UpdateData();
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
            BillTable.Add(new BillEntry());
        }

        private void RemoveBillItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<BillEntry>().ToList())
            {
                BillTable.Remove(item);
            }
        }

        private void MergeBillItems(IList<object> lst)
        {
            string groupName = GetGroupName();
            if (string.IsNullOrEmpty(groupName))
            {
                foreach (var item in lst.Cast<BillEntry>().ToList())
                {
                    // TODO: merge items
                    BillTable.Remove(item);
                }
            }
        }

        private void AddSpecificationItem(object obj)
        {
            SpecificationTable.Add(new SpecificationEntry());
        }

        private void RemoveSpecificationItems(IList<object> lst)
        {
            foreach (var item in lst.Cast<SpecificationEntry>().ToList())
            {
                SpecificationTable.Remove(item);
            }
        }

        private void MergeSpecificationItems(IList<object> lst)
        {
            string groupName = GetGroupName();
            if (string.IsNullOrEmpty(groupName))
            {
                foreach (var item in lst.Cast<SpecificationEntry>().ToList())
                {
                    // TODO: merge items
                    SpecificationTable.Remove(item);
                }
            }
        }

        private void SaveGraphValues(GraphType tp)
        {
            // TODO: Save updated graph values
        }

        #endregion Commands impl

        private void UpdateSelectedDocument()
        {
            if (_selectedTreeItem == null)
            {
                return;
            }

            IsGeneralGraphValuesVisible.Value = _selectedTreeItem.Header.Equals("Документы"); 
            IsSpecificationTableVisible.Value = SpecificationGroups.Contains(_selectedTreeItem);
            IsBillTableVisible.Value = BillGroups.Contains(_selectedTreeItem);
        }

        private string GetGroupName()
        {
            NewGroup newGroupDlg = new NewGroup();
            var result = newGroupDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return newGroupDlg.GroupName.Text;
            }
            return null;
        }

        private string GetMainFileName(string[] aFileNames)
        {
            SelectMainFile mainFileDlg = new SelectMainFile();
            mainFileDlg.ItemsSource = aFileNames;
            var result = mainFileDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return mainFileDlg.FileName.Text;
            }
            return null;
        }

        private void UpdateData()
        {
            BillGroups.Clear();
            BillGroups.AddRange(_docManager.XmlManager.BillGroups);

            SpecificationGroups.Clear();
            SpecificationGroups.AddRange(_docManager.XmlManager.SpecificationGroups);

            GeneralGraphValues.Clear();
            GeneralGraphValues.AddRange(_docManager.XmlManager.GeneralGraphValues);
        }
    }
}
