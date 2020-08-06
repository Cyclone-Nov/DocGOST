using GostDOC.Common;
using GostDOC.Models;
using GostDOC.UI;
using GostDOC.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private string _filePath = null;
                
        private DocManager _docManager = DocManager.Instance;
        private ProjectWrapper _project = new ProjectWrapper();
        private List<MoveInfo> _moveInfo = new List<MoveInfo>();

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<ComponentVM> ComponentsSelectedItem { get; } = new ObservableProperty<ComponentVM>();
        public ObservableCollection<ComponentVM> Components { get; } = new ObservableCollection<ComponentVM>();
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();
        public ObservableProperty<byte[]> CurrentPdfData { get; } = new ObservableProperty<byte[]>();
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValueVM> GeneralGraphValues { get; } = new ObservableCollection<GraphValueVM>();
        public ObservableCollection<Node> DocNodes { get; } = new ObservableCollection<Node>();
        public ObservableProperty<bool> IsAddEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsAutoSortEnabled { get; } = new ObservableProperty<bool>(true);

        public DocType CurrentDocType = DocType.Specification;

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
        public ICommand UpComponentsCmd => new Command<IList<object>>(UpComponents);
        public ICommand DownComponentsCmd => new Command<IList<object>>(DownComponents);
        public ICommand UpdatePdfCmd => new Command(UpdatePdf);
        

        /// <summary>
        /// Current selected configuration
        /// </summary>
        public string ConfigurationName
        {
            get
            {
                if (_selectedItem?.NodeType == NodeType.Configuration)
                {
                    return _selectedItem.Name;
                }
                if (_selectedItem?.NodeType == NodeType.Group)
                {
                    return _selectedItem.Parent.Name;
                }
                if (_selectedItem?.NodeType == NodeType.SubGroup)
                {
                    return _selectedItem.Parent.Parent.Name;
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// Current selected group
        /// </summary>
        public string GroupName
        {
            get
            {
                string name = string.Empty;
                if (_selectedItem?.NodeType == NodeType.Group)
                {
                    name = _selectedItem.Name;
                }
                else if (_selectedItem?.NodeType == NodeType.SubGroup)
                {
                    name = _selectedItem.Parent.Name;
                }
                if (name.Equals(Constants.DefaultGroupName))
                {
                    name = string.Empty;
                } 
                return name;
            }
        }
        /// <summary>
        /// Current selected subgroup
        /// </summary>
        public string SubGroupName => _selectedItem?.NodeType == NodeType.SubGroup ? _selectedItem.Name : string.Empty;

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
                // Reset file path
                _filePath = null;

                string mainFileName = null;
                if (open.FileNames.Length > 1)
                {
                    mainFileName = CommonDialogs.GetMainFileName(open.SafeFileNames);
                    if (string.IsNullOrEmpty(mainFileName))
                        return;
                }
                else
                {
                    // Save current file name only if one file was selected
                    _filePath = open.FileName;
                }

                // Parse xml files
                if (_docManager.LoadData(open.FileNames, mainFileName))
                {
                    // Update visual data
                    UpdateData();                    
                }
            }
        }

        private void SaveFile(object obj = null)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                SaveFileAs();
            }
            else
            {
                SaveFile();
            }
        }

        private void SaveFileAs(object obj = null)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "All Files *.xml | *.xml";
            save.Title = "Сохранить файл";

            if (save.ShowDialog() == DialogResult.OK)
            {
                _filePath = save.FileName;
                SaveFile();
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
            var groups = _project.GetGroupNames(ConfigurationName, _selectedItem.ParentType);
            var subGroupInfo = CommonDialogs.SelectGroup(groups);
            if (subGroupInfo != null)
            {
                MoveInfo moveInfo = new MoveInfo()
                {
                    Source = new SubGroupInfo() { GroupName = GroupName, SubGroupName = SubGroupName },
                    Destination = subGroupInfo
                };

                if (!moveInfo.Source.Equals(moveInfo.Destination))
                {
                    foreach (var cmp in lst.Cast<ComponentVM>().ToList())
                    {
                        // Add component to move list
                        Component component = new Component(cmp.Guid);
                        UpdateComponent(cmp, component);
                        moveInfo.Components.Add(component);
                        // Remove component from view
                        Components.Remove(cmp);
                    }
                    // Add move info
                    _moveInfo.Add(moveInfo);
                }
            }
        }

        private void SaveComponents(object obj)
        {
            string cfgName = ConfigurationName;

            // Update components properties
            List<Component> components = new List<Component>();
            foreach (var cmp in Components)
            {
                Component component = new Component(cmp.Guid);
                UpdateComponent(cmp, component);
                components.Add(component);
            }

            if (IsAutoSortEnabled.Value)
            {
                SortType sortType = Utils.GetSortType(_selectedItem.ParentType, GroupName);
                ISort<Component> sorter = SortFactory.GetSort(sortType);
                if (sorter != null)
                {
                    components = sorter.Sort(components);
                }
            }

            // Move components
            foreach (var move in _moveInfo)
            {
                _project.MoveComponents(cfgName, _selectedItem.ParentType, move);
            }
            _moveInfo.Clear();

            // Update components
            var groupInfo = new SubGroupInfo(GroupName, SubGroupName);
            var groupData = new GroupData(IsAutoSortEnabled.Value, components);

            _project.UpdateGroup(cfgName, _selectedItem.ParentType, groupInfo, groupData);

            UpdateGroupData();
        }

        private void SaveGraphValues(GraphPageType tp)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (var value in GeneralGraphValues)
            {
                values.Add(value.Name.Value, value.Text.Value);
            }
            _project.SaveGraphValues(values);
        }

        private void AddGroup(object obj)
        {
            string name = CommonDialogs.GetGroupName();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            // Add group or subgroup
            if (_selectedItem.NodeType == NodeType.Configuration)
            {
                _project.AddGroup(_selectedItem.Name, _selectedItem.ParentType, new SubGroupInfo(name, null));
            }
            else if (_selectedItem.NodeType == NodeType.Group)
            {
                _project.AddGroup(_selectedItem.Parent.Name, _selectedItem.ParentType, new SubGroupInfo(_selectedItem.Name, name));
            }
            // Update view
            UpdateGroups(_selectedItem.ParentType == NodeType.Specification, _selectedItem.ParentType == NodeType.Bill);
        }

        private void RemoveGroup(object obj)
        {
            bool removeComponents = true;
            // Ask user to remove components in group
            var groupData = GetGroupData();
            if (groupData?.Components.Count > 0)
            {
                var result = System.Windows.MessageBox.Show("Удалить компоненты?", "Удаление группы", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                removeComponents = (result == MessageBoxResult.Yes);
            }
            // Remove group or subgroup
            if (_selectedItem.NodeType == NodeType.Group)
            {
                var groupInfo = new SubGroupInfo(_selectedItem.Name, null);
                _project.RemoveGroup(_selectedItem.Parent.Name, _selectedItem.ParentType, groupInfo, removeComponents);
            }
            else if (_selectedItem.NodeType == NodeType.SubGroup)
            {
                var groupInfo = new SubGroupInfo(_selectedItem.Parent.Name, _selectedItem.Name);
                _project.RemoveGroup(_selectedItem.Parent.Parent.Name, _selectedItem.ParentType, groupInfo, removeComponents);
            }
            // Update view
            UpdateGroups(_selectedItem.ParentType == NodeType.Specification, _selectedItem.ParentType == NodeType.Bill);
        }

        private void UpComponents(IList<object> lst)
        {
            // Move selected components up
            var items = lst.Cast<ComponentVM>();
            foreach (var item in items)
            {
                int pos = Components.IndexOf(item);
                if (pos > 0)
                {
                    Components.Move(pos, pos - 1);
                }
            }
        }

        private void DownComponents(IList<object> lst)
        {
            // Move selected components down
            var items = lst.Cast<ComponentVM>().Reverse();
            foreach (var item in items)
            {
                int pos = Components.IndexOf(item);
                if (pos < Components.Count - 1)
                {
                    Components.Move(pos, pos + 1);
                }
            }
        }


        private void UpdatePdf(object obj)
        {
            var type = Common.Converters.GetPdfType(_selectedItem.ParentType);

            // TODO: async
            /*res = await*/ _docManager.SaveChangesInPdf(type);

            //  = _docManager.GetPdfStream(type);
        }

        private void UpdatePdf(object obj)
        {
            byte[] data = File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "example.pdf"));
            CurrentPdfData.Value = data;
            //CurrentPdfPath.Value = Path.Combine(Environment.CurrentDirectory, "example.pdf");
        }

        #endregion Commands impl

        private bool SaveFile()
        {
            return string.IsNullOrEmpty(_filePath) ? false : _docManager.SaveData(_filePath);
        }

        private void UpdateSelectedDocument()
        {
            if (_selectedItem == null)
            {
                return;
            }
            // Is group or subgroup selected
            bool isGroup = _selectedItem.NodeType == NodeType.Group || _selectedItem.NodeType == NodeType.SubGroup;
            // Is graph table visible
            IsGeneralGraphValuesVisible.Value = _selectedItem.NodeType == NodeType.Root;
            // Is add group button enabled
            IsAddEnabled.Value = (_selectedItem.NodeType == NodeType.Configuration || _selectedItem.NodeType == NodeType.Group) && 
                _selectedItem.Name != Constants.DefaultGroupName && _selectedItem.Name != Constants.GroupNameDoc;
            // Is remove group button enabled
            IsRemoveEnabled.Value = isGroup && _selectedItem.Name != Constants.DefaultGroupName;
            // Is scecification table visible
            IsSpecificationTableVisible.Value = _selectedItem.ParentType == NodeType.Specification && isGroup;
            // Is bill table visible
            IsBillTableVisible.Value = _selectedItem.ParentType == NodeType.Bill && isGroup;

            if (isGroup)
            {
                // Update selected group / subgroup components
                UpdateGroupData();
            }
            // Clear move components info
            _moveInfo.Clear();
        }

        private GroupData GetGroupData()
        {
            var groupInfo = new SubGroupInfo(GroupName, SubGroupName);
            return _project.GetGroupData(ConfigurationName, _selectedItem.ParentType, groupInfo);
        }

        private void UpdateGroupData()
        {
            var groupData = GetGroupData();

            IsAutoSortEnabled.Value = groupData.AutoSort;

            Components.Clear();
            foreach (var component in groupData.Components)
            {
                Components.Add(new ComponentVM(component));
            }            
        }

        private void UpdateConfiguration(Node aCollection, string aCfgName, IDictionary<string, Group> aGroups)
        {
            // Populate configuration tree
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
            // Add graph values to view
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
            // Clear group nodes
            if (aUpdateSp)
            {
                _specification.Nodes.Clear();
            }
            if (aUpdateB)
            {
                _bill.Nodes.Clear();
            }
            // Update visual view
            foreach (var cfg in _docManager.Project.Configurations)
            {
                if (aUpdateSp)
                {
                    UpdateConfiguration(_specification, cfg.Key, cfg.Value.Specification);
                }
                if (aUpdateB)
                {
                    UpdateConfiguration(_bill, cfg.Key, cfg.Value.Bill);
                }
            }
        }

        private void UpdateData()
        {
            UpdateGraphValues();
            UpdateGroups();
        }

        private void UpdateComponent(ComponentVM aSrc, Component aDst)
        {
            string groupName = GroupName;
            bool isDocument = groupName == Constants.GroupNameDoc;

            aDst.Type = isDocument ? ComponentType.Document : ComponentType.Component;
            aDst.Properties.Add(Constants.GroupNameSp, groupName);
            aDst.Properties.Add(Constants.ComponentName, aSrc.Name.Value);
            aDst.Properties.Add(Constants.ComponentSign, aSrc.Sign.Value);
            aDst.Properties.Add(Constants.ComponentProductCode, aSrc.Code.Value);
            aDst.Properties.Add(Constants.ComponentFormat, aSrc.Format.Value);
            aDst.Properties.Add(Constants.ComponentDoc, aSrc.Entry.Value);
            aDst.Properties.Add(Constants.ComponentSupplier, aSrc.Manufacturer.Value);
            aDst.Properties.Add(Constants.ComponentCountDev, aSrc.CountDev.Value.ToString());
            aDst.Properties.Add(Constants.ComponentCountSet, aSrc.CountSet.Value.ToString());
            aDst.Properties.Add(Constants.ComponentCountReg, aSrc.CountReg.Value.ToString());

            if (aSrc.NoteSP.Value != aSrc.DesignatorID.Value)
            {
                aDst.Properties.Add(Constants.ComponentNote, aSrc.NoteSP.Value);
            }
            else
            {
                aDst.Properties.Add(Constants.ComponentNote, aSrc.Note.Value);
            }
        }
    }
}
