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

        private Node _elements = new Node() { Name = "Перечень элементов", NodeType = NodeType.Elements, Nodes = new ObservableCollection<Node>() };
        private Node _specification = new Node() { Name = "Спецификация", NodeType = NodeType.Specification, Nodes = new ObservableCollection<Node>() };
        private Node _bill = new Node() { Name = "Ведомость покупных изделий", NodeType = NodeType.Bill, Nodes = new ObservableCollection<Node>() };
        private Node _bill_D27 = new Node() { Name = "Ведомость Д27", NodeType = NodeType.Bill_D27, Nodes = new ObservableCollection<Node>() };
        private Node _selectedItem = null;

        private DocManager _docManager = DocManager.Instance;

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<ComponentVM> ComponentsSelectedItem { get; } = new ObservableProperty<ComponentVM>();
        public ObservableCollection<ComponentVM> Components { get; } = new ObservableCollection<ComponentVM>();
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValueVM> GeneralGraphValues { get; } = new ObservableCollection<GraphValueVM>();
        public ObservableCollection<Node> DocNodes { get; } = new ObservableCollection<Node>();

        #region Commands
        public ICommand OpenFilesCmd => new Command(OpenFiles);
        public ICommand SaveFileCmd => new Command(SaveFile);
        public ICommand SaveFileAsCmd => new Command(SaveFileAs);
        public ICommand ExitCmd => new Command<Window>(Exit);
        public ICommand AddComponentCmd => new Command(AddComponent);
        public ICommand RemoveComponentsCmd => new Command<IList<object>>(RemoveComponents);
        public ICommand MoveComponentsCmd => new Command<IList<object>>(MoveComponents);
        public ICommand TreeViewSelectionChangedCmd => new Command<Node>(TreeViewSelectionChanged);
        public ICommand SaveGraphValuesCmd => new Command<GraphPageType>(SaveGraphValues);

        #endregion Commands

        public MainWindowVM()
        {
            var root = new Node() { Name = "Документы", Nodes = new ObservableCollection<Node>() };
            root.Nodes.Add(_elements);
            root.Nodes.Add(_specification);
            root.Nodes.Add(_bill);
            root.Nodes.Add(_bill_D27);

            DocNodes.Add(root);
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
                _docManager.LoadData(open.FileNames, mainFileName);
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

        private void TreeViewSelectionChanged(Node obj)
        {
            _selectedItem = obj;
            UpdateSelectedDocument();
        }

        private void Exit(Window wnd)
        {
            wnd.Close();
        }
        
        private void AddComponent(object obj)
        {
            Components.Add(new ComponentVM());
        }

        private void RemoveComponents(IList<object> lst)
        {
            foreach (var item in lst.Cast<ComponentVM>().ToList())
            {
                Components.Remove(item);
            }
        }

        private void MoveComponents(IList<object> lst)
        {
            string groupName = GetGroupName();
            if (string.IsNullOrEmpty(groupName))
            {
                foreach (var item in lst.Cast<ComponentVM>().ToList())
                {
                    // TODO: move items
                    Components.Remove(item);
                }
            }
        }

        private void SaveGraphValues(GraphPageType tp)
        {
            // TODO: Save updated graph values
        }

        #endregion Commands impl
        private void UpdateSelectedDocument()
        {
            if (_selectedItem == null)
            {
                return;
            }

            bool isGroup = _selectedItem.NodeType == NodeType.Group || _selectedItem.NodeType == NodeType.SubGroup;
            IsGeneralGraphValuesVisible.Value = _selectedItem.NodeType == NodeType.Root;
            IsSpecificationTableVisible.Value = _selectedItem.ParentType == NodeType.Specification && isGroup;
            IsBillTableVisible.Value = _selectedItem.ParentType == NodeType.Bill && isGroup;

            if (isGroup)
            {
                UpdateComponents();
            }
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

        private void UpdateComponents()
        {
            Components.Clear();

            string cfgName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Parent.Name : _selectedItem.Parent.Parent.Name;
            string groupName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Name : _selectedItem.Parent.Name;
            if (groupName == "Без имени")
                groupName = "";

            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(cfgName, out cfg))
            {
                Group grp = null;
                // Find group
                if (_selectedItem.ParentType == NodeType.Specification)
                {
                    cfg.Specification.TryGetValue(groupName, out grp);
                }
                else if (_selectedItem.ParentType == NodeType.Bill)
                {
                    cfg.Bill.TryGetValue(groupName, out grp);
                }
                // Find subgroup
                if (_selectedItem.NodeType == NodeType.SubGroup && grp != null)
                {
                    grp.SubGroups.TryGetValue(_selectedItem.Name, out grp);
                }
                // Fill components
                if (grp != null)
                {
                    foreach (var component in grp.Components.Values)
                    {
                        Components.Add(new ComponentVM(component));
                    }
                }
            }
        }

        private void UpdateConfiguration(Node aCollection, string aCfgName, IDictionary<string, Group> aGroups)
        {
            Node treeItemCfg = new Node() { Name = aCfgName, NodeType = NodeType.Configuration, ParentType = aCollection.NodeType, Parent = aCollection, Nodes = new ObservableCollection<Node>() };
            foreach (var grp in aGroups)
            {
                string groupName = grp.Key;
                if (string.IsNullOrEmpty(groupName))
                {
                    groupName = "Без имени";
                }

                Node treeItemGroup = new Node() { Name = groupName, NodeType = NodeType.Group, ParentType = aCollection.NodeType, Parent = treeItemCfg, Nodes = new ObservableCollection<Node>() };
                foreach (var sub in grp.Value.SubGroups)
                {
                    Node treeItemSubGroup = new Node() { Name = sub.Key, NodeType = NodeType.SubGroup, ParentType = aCollection.NodeType, Parent = treeItemGroup };
                    treeItemGroup.Nodes.Add(treeItemSubGroup);
                }
                treeItemCfg.Nodes.Add(treeItemGroup);

            }
            aCollection.Nodes.Add(treeItemCfg);

        }

        private void UpdateGraphValues()
        {
            GeneralGraphValues.Clear();
            foreach (var cfg in _docManager.Project.Configurations)
            {
                if (cfg.Value.Graphs.Count > 0)
                {
                    foreach (var graph in cfg.Value.Graphs)
                    {
                        GeneralGraphValues.Add(new GraphValueVM(graph.Key, graph.Value));
                    }
                    break;
                }
            }
        }

        private void UpdateGroups()
        {
            _specification.Nodes.Clear();
            _bill.Nodes.Clear();

            foreach (var cfg in _docManager.Project.Configurations)
            {
                UpdateConfiguration(_specification, cfg.Key, cfg.Value.Specification);
                UpdateConfiguration(_bill, cfg.Key, cfg.Value.Bill);
            }
        }

        private void UpdateData()
        {
            UpdateGraphValues();
            UpdateGroups();
        }
    }
}
