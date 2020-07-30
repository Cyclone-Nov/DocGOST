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
        private ProjectWrapper _project = new ProjectWrapper();

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<ComponentVM> ComponentsSelectedItem { get; } = new ObservableProperty<ComponentVM>();
        public ObservableCollection<ComponentVM> Components { get; } = new ObservableCollection<ComponentVM>();
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValueVM> GeneralGraphValues { get; } = new ObservableCollection<GraphValueVM>();
        public ObservableCollection<Node> DocNodes { get; } = new ObservableCollection<Node>();
        public ObservableProperty<bool> IsAddEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);

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
        public ICommand AddGroupCmd => new Command(AddGroup);
        public ICommand RemoveGroupCmd => new Command(RemoveGroup);
        public ICommand SaveComponentsCmd => new Command(SaveComponents);
        public ICommand UpComponentCmd => new Command<ComponentVM>(UpComponent);
        public ICommand DownComponentCmd => new Command<ComponentVM>(DownComponent);

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

        private void SaveComponents(object obj)
        {
            string cfgName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Parent.Name : _selectedItem.Parent.Parent.Name;
            string groupName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Name : _selectedItem.Parent.Name;
            string subGroupName = _selectedItem.NodeType == NodeType.SubGroup ? _selectedItem.Name : string.Empty;

            bool isDocument = groupName == Constants.GroupNameDoc || subGroupName == Constants.GroupNameDoc;

            List<Component> components = new List<Component>();
            foreach (var cmp in Components)
            {
                Component component = new Component(cmp.Guid)
                {                    
                    Type = isDocument ? ComponentType.Document : ComponentType.Component
                };                

                component.Properties.Add(Constants.GroupNameSp, groupName);
                component.Properties.Add(Constants.ComponentName, cmp.Name.Value);
                component.Properties.Add(Constants.ComponentSign, cmp.Sign.Value);
                component.Properties.Add(Constants.ComponentProductCode, cmp.Code.Value);
                component.Properties.Add(Constants.ComponentFormat, cmp.Format.Value);
                component.Properties.Add(Constants.ComponentDoc, cmp.Entry.Value);
                component.Properties.Add(Constants.ComponentSupplier, cmp.Manufacturer.Value);
                component.Properties.Add(Constants.ComponentCountDev, cmp.CountDev.Value.ToString());
                component.Properties.Add(Constants.ComponentCountSet, cmp.CountSet.Value.ToString());
                component.Properties.Add(Constants.ComponentCountReg, cmp.CountReg.Value.ToString());
                component.Properties.Add(Constants.ComponentNote, cmp.Note.Value);

                components.Add(component);
            }

            _project.UpdateComponents(cfgName, groupName, subGroupName, _selectedItem.ParentType, components);
        }

        private void SaveGraphValues(GraphPageType tp)
        {
            // TODO: Save updated graph values
        }

        private void AddGroup(object obj)
        {
            string name = GetGroupName();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (_selectedItem.NodeType == NodeType.Configuration)
            {
                _project.AddGroup(_selectedItem.Name, name, null, _selectedItem.ParentType);
            }
            else if (_selectedItem.NodeType == NodeType.Group)
            {
                _project.AddGroup(_selectedItem.Parent.Name, _selectedItem.Name, name, _selectedItem.ParentType);
            }

            UpdateGroups(_selectedItem.ParentType == NodeType.Specification, _selectedItem.ParentType == NodeType.Bill);
        }

        private void RemoveGroup(object obj)
        {
            bool removeComponents = false;
            var components = GetComponents();
            if (components.Count > 0)
            {
                var result = System.Windows.MessageBox.Show("Удалить компоненты?", "Удаление группы", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                removeComponents = (result == MessageBoxResult.Yes);
            }

            if (_selectedItem.NodeType == NodeType.Group)
            {
                _project.RemoveGroup(_selectedItem.Parent.Name, _selectedItem.Name, null, _selectedItem.ParentType, removeComponents);
            }
            else if (_selectedItem.NodeType == NodeType.SubGroup)
            {
                _project.RemoveGroup(_selectedItem.Parent.Parent.Name, _selectedItem.Parent.Name, _selectedItem.Name, _selectedItem.ParentType, removeComponents);
            }

            UpdateGroups(_selectedItem.ParentType == NodeType.Specification, _selectedItem.ParentType == NodeType.Bill);
        }

        private void UpComponent(ComponentVM obj)
        {
            if (obj != null)
            {
                int pos = Components.IndexOf(obj);
                if (pos > 0)
                {
                    Components.Move(pos, pos - 1);
                }
            }
        }

        private void DownComponent(ComponentVM obj)
        {
            if (obj != null)
            {
                int pos = Components.IndexOf(obj);
                if (pos < Components.Count - 1)
                {
                    Components.Move(pos, pos + 1);
                }
            }
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
            IsAddEnabled.Value = (_selectedItem.NodeType == NodeType.Configuration || _selectedItem.NodeType == NodeType.Group) && 
                _selectedItem.Name != Constants.DefaultGroupName && _selectedItem.Name != Constants.GroupNameDoc;
            IsRemoveEnabled.Value = isGroup && _selectedItem.Name != Constants.DefaultGroupName;

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

        private IList<Component> GetComponents()
        {
            string cfgName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Parent.Name : _selectedItem.Parent.Parent.Name;
            string groupName = _selectedItem.NodeType == NodeType.Group ? _selectedItem.Name : _selectedItem.Parent.Name;
            string subGroupName = _selectedItem.NodeType == NodeType.SubGroup ? _selectedItem.Name : string.Empty;

            if (groupName == Constants.DefaultGroupName)
                groupName = "";

            return _project.GetComponents(cfgName, _selectedItem.ParentType, groupName, subGroupName);
        }

        private void UpdateComponents()
        {
            Components.Clear();
            foreach (var component in GetComponents())
            {
                Components.Add(new ComponentVM(component));
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
                    groupName = Constants.DefaultGroupName;
                }

                Node treeItemGroup = new Node() { Name = groupName, NodeType = NodeType.Group, ParentType = aCollection.NodeType, Parent = treeItemCfg, Nodes = new ObservableCollection<Node>() };              
                foreach (var sub in grp.Value.SubGroups.AsNotNull())
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

        private void UpdateGroups(bool aUpdateSp = true, bool aUpdateB = true)
        {
            if (aUpdateSp) 
                _specification.Nodes.Clear();

            if (aUpdateB)
                _bill.Nodes.Clear();

            foreach (var cfg in _docManager.Project.Configurations)
            {
                if (aUpdateSp)
                    UpdateConfiguration(_specification, cfg.Key, cfg.Value.Specification);

                if (aUpdateB)
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
