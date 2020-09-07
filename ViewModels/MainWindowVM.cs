using GongSolutions.Wpf.DragDrop;
using GostDOC.Common;
using GostDOC.Events;
using GostDOC.Models;
using GostDOC.UI;
using GostDOC.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private bool _parseAssemblyUnitsSet = false;

        private DocManager _docManager = DocManager.Instance;

        private DocumentTypes _docTypes = new DocumentTypes();
        private MaterialTypes _materials = new MaterialTypes();

        private ProjectWrapper _project = new ProjectWrapper();
        private List<MoveInfo> _moveInfo = new List<MoveInfo>();

        private UndoRedoStack<IList<object>> _undoRedoComponents = new UndoRedoStack<IList<object>>();
        private UndoRedoStack<IList<object>> _undoRedoGraphs = new UndoRedoStack<IList<object>>();

        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<ComponentVM> ComponentsSelectedItem { get; } = new ObservableProperty<ComponentVM>();
        public ObservableCollection<ComponentVM> Components { get; } = new ObservableCollection<ComponentVM>();
        // Graphs
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValueVM> GeneralGraphValues { get; } = new ObservableCollection<GraphValueVM>();
        // Doc tree
        public ObservableCollection<Node> DocNodes { get; } = new ObservableCollection<Node>();
        // Context menu
        public ObservableCollection<MenuNode> TableContextMenu { get; } = new ObservableCollection<MenuNode>();
        public ObservableProperty<bool> TableContextMenuEnabled { get; } = new ObservableProperty<bool>(false);
        // PDF data
        public ObservableProperty<string> CurrentPdfPath { get; } = new ObservableProperty<string>();
        public ObservableProperty<byte[]> CurrentPdfData { get; } = new ObservableProperty<byte[]>();

        // Enable / disable buttons
        public ObservableProperty<bool> IsAddEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsAutoSortEnabled { get; } = new ObservableProperty<bool>(true);

        public DocType CurrentDocType = DocType.Specification;

        // Drag / drop
        public DragDropFile DragDropFile { get; } = new DragDropFile();

        #region Commands
        public ICommand UndoCmd => new Command<MenuNode>(Undo);
        public ICommand RedoCmd => new Command<MenuNode>(Redo);
        public ICommand NewFileCmd => new Command(NewFile);
        public ICommand OpenFileCmd => new Command(OpenFile);
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
        public ICommand ClickMenuCmd => new Command<MenuNode>(ClickMenu);
        public ICommand EditNameValueCmd => new Command<System.Windows.Controls.DataGrid>(EditNameValue);
        public ICommand EditComponentsCmd => new Command<System.Windows.Controls.DataGrid>(EditComponents);

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
            // Subscribe to drag and drop events
            DragDropFile.FileDropped += OnDragDropFile_FileDropped;
            // Subscribe to assembly unit found event
            _docManager.XmlManager.AssemblyUnitFound += OnAssemblyUnitFound;
            // Load document types
            _docTypes.Load();
            // Load material types
            _materials.Load();
        }

        #region Commands impl
        private void Undo(MenuNode obj)
        {
            if (_selectedItem.NodeType == NodeType.Root)
            {
                GeneralGraphValues.SetMementos(_undoRedoGraphs.Undo());
            }
            else if (!string.IsNullOrEmpty(GroupName))
            {
                Components.SetMementos(_undoRedoComponents.Undo());
            }
        }

        private void Redo(MenuNode obj)
        {
            if (_selectedItem.NodeType == NodeType.Root)
            {
                GeneralGraphValues.SetMementos(_undoRedoGraphs.Redo());
            }
            else if (!string.IsNullOrEmpty(GroupName))
            {
                Components.SetMementos(_undoRedoComponents.Redo());
            }
        }

        private bool locker = true;
        private void EditNameValue(System.Windows.Controls.DataGrid e)
        {
            if (locker)
            {
                try
                {
                    locker = false;
                    e.CommitEdit(DataGridEditingUnit.Row, false);
                    _undoRedoGraphs.Add(GeneralGraphValues.GetMementos());
                }
                finally
                {
                    locker = true;
                }
            }
        }

        private void EditComponents(System.Windows.Controls.DataGrid e)
        {
            if (locker)
            {
                try
                {
                    locker = false;
                    e.CommitEdit(DataGridEditingUnit.Row, false);
                    _undoRedoComponents.Add(Components.GetMementos());
                }
                finally
                {
                    locker = true;
                }
            }
        }

        private void OpenFile(object obj)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "xml Files *.xml | *.xml";
            open.Title = "Выбрать файл...";

            if (open.ShowDialog() == DialogResult.OK)
            {
                OpenFile(open.FileName);
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
            save.Filter = "xml Files *.xml | *.xml";
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
            bool removeComponents = false;
            if (_selectedItem.ParentType == NodeType.Specification)
            {
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
            }
          
            string name = string.Empty;
            SubGroupInfo groupInfo = null;
            
            // Remove group or subgroup 
            if (_selectedItem.NodeType == NodeType.Group)
            {
                groupInfo = new SubGroupInfo(_selectedItem.Name, null);
                name = _selectedItem.Parent.Name;
            }
            else if (_selectedItem.NodeType == NodeType.SubGroup)
            {
                groupInfo = new SubGroupInfo(_selectedItem.Parent.Name, _selectedItem.Name);
                name = _selectedItem.Parent.Parent.Name;
            }

            if (!string.IsNullOrEmpty(name) && groupInfo != null)
            {
                _project.RemoveGroup(name, _selectedItem.ParentType, groupInfo, removeComponents);
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
            if (_selectedItem == null)
                return;
            var nodeType = _selectedItem.ParentType;
            if (nodeType == NodeType.Root)
                nodeType = _selectedItem.NodeType;
            var type = Common.Converters.GetPdfType(nodeType);

            if(nodeType == NodeType.Root)
            {
                System.Windows.MessageBox.Show("Документ для отображения не выбран! Выберите в дереве документ для отображения");
                return;
            }
            
            // TODO: async
            /*res = await*/ _docManager.SaveChangesInPdf(type);
            CurrentPdfData.Value = _docManager.GetPdfData(type);
        }

        private void ClickMenu(MenuNode obj)
        {
            if (GroupName.Equals(Constants.GroupDoc))
            {
                Document doc = _docTypes.GetDocument(obj?.Parent?.Name, obj?.Name);
                if (doc != null)
                {
                    ComponentVM cmp = new ComponentVM();
                    cmp.Name.Value = doc.Name;
                    cmp.Sign.Value = _project.GetGraphValue(ConfigurationName, Constants.GraphSign) + doc.Code;
                    cmp.Format.Value = "A4";
                    Components.Add(cmp);
                }        
            }
        }

        #endregion Commands impl

        private void OnDragDropFile_FileDropped(object sender, TEventArgs<string> e)
        {
            OpenFile(e.Arg);
        }

        private void NewFile(object obj)
        {
            CommonDialogs.CreateConfiguration();
            UpdateData();
        }

        private void OpenFile(string aFilePath)
        {
            // Reset parse assebly units flag
            _parseAssemblyUnitsSet = false;
            // Save current file name only if one file was selected
            _filePath = aFilePath;

            // Parse xml files
            if (_docManager.LoadData(_filePath))
            {
                // Update visual data
                UpdateData();
            }
            else
            {
                System.Windows.MessageBox.Show("Формат файла не поддерживается!", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

            if (_selectedItem.ParentType == NodeType.Specification)
            {
                // Is add group button enabled
                IsAddEnabled.Value = _selectedItem.NodeType == NodeType.Group && _selectedItem.Name != Constants.DefaultGroupName;
                
                // Is remove group button enabled
                IsRemoveEnabled.Value = _selectedItem.NodeType == NodeType.SubGroup && _selectedItem.Name != Constants.DefaultGroupName;
            }
            else if (_selectedItem.ParentType == NodeType.Bill)
            {
                // Is add group button enabled
                IsAddEnabled.Value = (_selectedItem.NodeType == NodeType.Configuration || _selectedItem.NodeType == NodeType.Group) &&
                                      _selectedItem.Name != Constants.DefaultGroupName;

                // Is remove group button enabled
                IsRemoveEnabled.Value = isGroup && _selectedItem.Name != Constants.DefaultGroupName;
            }
            else
            {
                IsAddEnabled.Value = false;
                IsRemoveEnabled.Value = false;
            }

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

            UpdateTableContextMenu();
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

            // Fill components
            Components.Clear();
            foreach (var component in groupData.Components)
            {
                Components.Add(new ComponentVM(component));
            }

            // Add initial value to undo / redo stack
            _undoRedoComponents.Clear();
            _undoRedoComponents.Add(Components.GetMementos());
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

            // Add initial value to undo / redo stack
            _undoRedoGraphs.Clear();
            _undoRedoGraphs.Add(GeneralGraphValues.GetMementos());
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
            bool isDocument = groupName == Constants.GroupDoc;

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

        private void OnAssemblyUnitFound(object sender, EventArgs e)
        {
            if (_parseAssemblyUnitsSet)
            {
                // Already asked
                return;
            }

            // Ask user
            var result = System.Windows.MessageBox.Show("Загрузить связанные компоненты (режим ВП)?", "Загрузка", MessageBoxButton.YesNo);
            // Set parse type
            if (result == MessageBoxResult.Yes)
            {
                _docManager.Project.Type = ProjectType.GostDocB;
                _docManager.XmlManager.ParseAssemblyUnits = true;
            }
            else
            {
                _docManager.XmlManager.ParseAssemblyUnits = false;
            }

            // Set assebly units asked flag
            _parseAssemblyUnitsSet = true;
        }

        private void UpdateTableContextMenu()
        {
            TableContextMenu.Clear();
            TableContextMenuEnabled.Value = false;

            if (GroupName.Equals(Constants.GroupDoc))
            {
                foreach (var kvp in _docTypes.Documents)
                {
                    MenuNode node = new MenuNode() { Name = kvp.Key, Nodes = new ObservableCollection<MenuNode>() };
                    foreach (var doc in kvp.Value)
                    {
                        node.Nodes.Add(new MenuNode() { Name = doc.Key, Parent = node });
                    }
                    TableContextMenu.Add(node);
                }
                TableContextMenuEnabled.Value = true;
            }
            else if (GroupName.Equals(Constants.GroupMaterials))
            {
                foreach (var kvp in _materials.Materials)
                {
                    MenuNode node = new MenuNode() { Name = kvp.Key, Nodes = new ObservableCollection<MenuNode>() };
                    foreach (var doc in kvp.Value)
                    {
                        node.Nodes.Add(new MenuNode() { Name = doc.Key, Parent = node });
                    }
                    node.Nodes.Add(new MenuNode() { Name = "<Новый материал>", Parent = node });
                    TableContextMenu.Add(node);
                }
                TableContextMenuEnabled.Value = true;
            }
        }
    }
}
