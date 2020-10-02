using GongSolutions.Wpf.DragDrop;
using GostDOC.Common;
using GostDOC.Events;
using GostDOC.ExcelExport;
using GostDOC.Models;
using GostDOC.UI;
using GostDOC.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GostDOC.ViewModels
{
    class MainWindowVM
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private Node _elements = new Node() { Name = "Перечень элементов", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _specification = new Node() { Name = "Спецификация", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _bill = new Node() { Name = "Ведомость покупных изделий", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _bill_D27 = new Node() { Name = "Ведомость Д27", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };

        private DocType _docType = DocType.None;

        private Node _selectedItem = null;
        private string _filePath = null;
        private bool _shouldSave = false;

        private DocManager _docManager = DocManager.Instance;

        private DocumentTypes _docTypes = DocManager.Instance.DocumentTypes;
        private MaterialTypes _materials = DocManager.Instance.MaterialTypes;

        private ProjectWrapper _project = new ProjectWrapper();
        private List<MoveInfo> _moveInfo = new List<MoveInfo>();

        private UndoRedoStack<IList<object>> _undoRedoComponents = new UndoRedoStack<IList<object>>();
        private UndoRedoStack<IList<object>> _undoRedoGraphs = new UndoRedoStack<IList<object>>();

        private ExcelManager _excelManager = new ExcelManager();

        public ObservableProperty<string> Title { get; } = new ObservableProperty<string>();
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
        public ObservableProperty<bool> IsUndoEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRedoEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsSaveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsExportExcelEnabled { get; } = new ObservableProperty<bool>(false);

        // Drag / drop
        public DragDropFile DragDropFile { get; } = new DragDropFile();

        #region Commands
        public ICommand UndoCmd => new Command<MenuNode>(Undo, IsUndoEnabled);
        public ICommand RedoCmd => new Command<MenuNode>(Redo, IsRedoEnabled);
        public ICommand NewFileCmd => new Command(NewFile);
        public ICommand OpenFileSpCmd => new Command(OpenFileSp);
        public ICommand OpenFileBCmd => new Command(OpenFileB);
        public ICommand OpenFileD27Cmd => new Command(OpenFileD27);
        public ICommand OpenFileElCmd => new Command(OpenFileEl);
        public ICommand SaveFileCmd => new Command(SaveFile, IsSaveEnabled);
        public ICommand SaveFileAsCmd => new Command(SaveFileAs);
        public ICommand ClosingCmd => new Command<System.ComponentModel.CancelEventArgs>(Closing);
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
        public ICommand EditNameValueCmd => new Command<DataGrid>(EditNameValue);
        public ICommand EditComponentsCmd => new Command<DataGrid>(EditComponents);
        public ICommand ExportPDFCmd => new Command(ExportPDF, IsSaveEnabled);
        public ICommand ExportExcelCmd => new Command(ExportExcel, IsExportExcelEnabled);
        public ICommand ImportMaterialsCmd => new Command(ImportMaterials);
        public ICommand SaveMaterialsCmd => new Command(SaveMaterials);
        public ICommand UpdateMaterialsCmd => new Command(UpdateMaterials);
        public ICommand CopyCellCmd => new Command<DataGridCellInfo>(CopyCell);
        public ICommand PasteCellCmd => new Command<DataGridCellInfo>(PasteCell);

        public string WindowTitle
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return "ПО формирования документов на основе перечня элементов изделия " + version;
            }
        }
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
            _docManager.Load();
            // Subscribe to drag and drop events
            DragDropFile.FileDropped += OnDragDropFile_FileDropped;
            // Update title
            UpdateTitle();
        }

        #region Commands impl

        private void CopyCell(DataGridCellInfo cellInfo)
        {
            var txt = (cellInfo.Column?.GetCellContent(cellInfo.Item) as TextBlock)?.Text;
            if (txt != null)
            {
                Clipboard.SetText(txt);
            }
        }

        private void PasteCell(DataGridCellInfo cellInfo)
        {
            var txtBlock = (cellInfo.Column?.GetCellContent(cellInfo.Item) as TextBlock);
            if (txtBlock != null)
            {
                txtBlock.Text = Clipboard.GetText();
            }
        }

        private void Undo(MenuNode obj)
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.NodeType == NodeType.Root)
                {
                    GeneralGraphValues.SetMementos(_undoRedoGraphs.Undo());
                    UpdateUndoRedoMenu(_undoRedoGraphs);
                }
                else if (!string.IsNullOrEmpty(GroupName))
                {
                    Components.SetMementos(_undoRedoComponents.Undo());
                    UpdateUndoRedoMenu(_undoRedoComponents);
                }
            }
        }  

        private void Redo(MenuNode obj)
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.NodeType == NodeType.Root)
                {
                    GeneralGraphValues.SetMementos(_undoRedoGraphs.Redo());
                    UpdateUndoRedoMenu(_undoRedoGraphs);
                }
                else if (!string.IsNullOrEmpty(GroupName))
                {
                    Components.SetMementos(_undoRedoComponents.Redo());
                    UpdateUndoRedoMenu(_undoRedoComponents);
                }
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
                    UpdateUndoRedoGraph();
                    IsUndoEnabled.Value = true;
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
                    UpdateUndoRedoComponents();
                    IsUndoEnabled.Value = true;
                }
                finally
                {
                    locker = true;
                }
            }
        }

        private void OpenFileSp(object obj)
        {
            OpenFile(_specification, DocType.Specification);
        }

        private void OpenFileB(object obj)
        {
            OpenFile(_bill, DocType.Bill);
        }

        private void OpenFileD27(object obj)
        {
            OpenFile(_bill_D27, DocType.D27);
        }

        private void OpenFileEl(object obj)
        {
            OpenFile(_elements, DocType.ItemsList);
        }

        private void NewFile(object obj)
        {
            if (!SavePreviousFile())
            {
                // Operation cancelled
                return;
            }

            if (CommonDialogs.CreateConfiguration())
            {
                _shouldSave = true;
                IsSaveEnabled.Value = true;

                DocNodes.Clear();
                DocNodes.Add(_specification);
                
                _filePath = string.Empty;

                UpdateDocType(DocType.Specification);
                UpdateTitle();
                UpdateData();
                HideTables();
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
                Save();
            }            
        }

        private void SaveFileAs(object obj = null)
        {
            var path = CommonDialogs.SaveFileAs("xml Files *.xml | *.xml", "Сохранить файл");
            if (!string.IsNullOrEmpty(path))
            {
                _filePath = path;
                // Save file path to title
                UpdateTitle();
                // Save file
                Save();
            }
        }

        private void TreeViewSelectionChanged(Node obj)
        {
            _selectedItem = obj;
            UpdateSelectedDocument();
        }

        private void Closing(System.ComponentModel.CancelEventArgs e)
        {
            if (_shouldSave)
            {
                var result = System.Windows.MessageBox.Show("Сохранить изменения в xml файле?", "Сохранение изменений", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveFile();
                        break;
                    case MessageBoxResult.No:
                        _shouldSave = false;
                        break;
                }

                if (_shouldSave && e != null)
                {
                    e.Cancel = true;
                }
            }
        }
        
        private void AddComponent(object obj)
        {
            Components.Add(new ComponentVM());
            UpdateUndoRedoComponents();
        }

        private void RemoveComponents(IList<object> lst)
        {
            foreach (var item in lst.Cast<ComponentVM>().ToList())
            {
                Components.Remove(item);
            }
            UpdateUndoRedoComponents();
        }

        private void MoveComponents(IList<object> lst)
        {
            var groups = _project.GetGroupNames(ConfigurationName, _docType);
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
            _shouldSave = true;

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
                _project.MoveComponents(cfgName, _docType, move);
            }
            _moveInfo.Clear();

            // Update components
            var groupInfo = new SubGroupInfo(GroupName, SubGroupName);
            var groupData = new GroupData(IsAutoSortEnabled.Value, components);

            _project.UpdateGroup(cfgName, _docType, groupInfo, groupData);

            UpdateGroupData();
        }

        private void SaveGraphValues(GraphPageType tp)
        {
            _shouldSave = true;

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
                _project.AddGroup(_selectedItem.Name, _docType, new SubGroupInfo(name, null));
            }
            else if (_selectedItem.NodeType == NodeType.Group)
            {
                _project.AddGroup(_selectedItem.Parent.Name, _docType, new SubGroupInfo(_selectedItem.Name, name));
            }
            // Update view
            UpdateGroups();
        }

        private void RemoveGroup(object obj)
        {
            bool removeComponents = false;
            if (_docType == DocType.Specification)
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
                _project.RemoveGroup(name, _docType, groupInfo, removeComponents);
            }

            // Update view
            UpdateGroups();
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
            
            // TODO: async
            /*res = await*/ 
            if(_docManager.PrepareData(_docType))
            { 
                _docManager.PreparePDF(_docType);
                CurrentPdfData.Value = _docManager.GetPdfData(_docType);
            }
        }

        private void ClickMenu(MenuNode obj)
        {
            if (obj == null)
            {
                return;
            }

            if (GroupName.Equals(Constants.GroupDoc))
            {
                Document doc = _docTypes.GetDocument(obj.Parent?.Name, obj.Name);
                if (doc != null)
                {
                    ComponentVM cmp = new ComponentVM();
                    cmp.Name.Value = doc.Name;
                    cmp.Sign.Value = _project.GetGraphValue(ConfigurationName, Constants.GraphSign) + doc.Code;
                    cmp.Format.Value = "A4";
                    Components.Add(cmp);
                }        
            }
            else if (GroupName.Equals(Constants.GroupMaterials))
            {
                Material material = null;
                if (obj.Name == Constants.NewMaterialMenuItem)
                {
                    // Add material
                    material = CommonDialogs.AddMaterial();

                    if (material != null)
                    {
                        if (_materials.AddMaterial(obj.Parent.Name, material))
                        {
                            // Add node
                            obj.Parent.Nodes.InsertSorted(new MenuNode() { Name = material.Name, Parent = obj.Parent });
                            // Save file
                            _materials.Save();
                        }
                    }
                }
                else
                {
                    // Find material
                    material = _materials.GetMaterial(obj.Parent?.Name, obj.Name);
                    if (material != null)
                    {
                        ComponentVM cmp = new ComponentVM();
                        cmp.Name.Value = material.Name;
                        cmp.NoteSP.Value = material.Note;
                        Components.Add(cmp);
                    }
                }
            }
        }

        private void ExportPDF(object obj)
        {
        }

        private void ExportExcel(object obj)
        {
            if (_excelManager.CanExport(_docType))
            {
                var path = CommonDialogs.SaveFileAs("Excel Files *.xlsx | *.xlsx", "Сохранить файл");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelManager.Export(_docType, path);
                }
            }
        }

        private void ImportMaterials(object obj)
        {
            var path = CommonDialogs.OpenFile("Material Files *.xml | *.xml", "Открыть файл материалов");
            if (!string.IsNullOrEmpty(path))
            {
                _materials.Import(path);
            }
        }

        private void SaveMaterials(object obj)
        {
            var path = CommonDialogs.SaveFileAs("Material Files *.xml | *.xml", "Сохранить файл материалов");
            if (!string.IsNullOrEmpty(path))
            {
                _materials.Save(path);
            }
        }

        private void UpdateMaterials(object obj)
        {
            CommonDialogs.CreateEditMaterials();
            UpdateTableContextMenu();
        }

        #endregion Commands impl

        private void OnDragDropFile_FileDropped(object sender, TEventArgs<string> e)
        {
            OpenFile(e.Arg);
        }

        private void OpenFile(Node aNode, DocType aDocType)
        {
            if (!SavePreviousFile())
            {
                // Operation cancelled
                return;
            }

            // Open new file
            string path = CommonDialogs.OpenFile("xml Files *.xml | *.xml", "Выбрать файл...");
            if (!string.IsNullOrEmpty(path))
            {
                UpdateDocType(aDocType);

                if (OpenFile(path))
                {
                    DocNodes.Clear();
                    DocNodes.Add(aNode);
                    IsSaveEnabled.Value = true;
                }
                else
                {
                    UpdateDocType(DocType.None);
                    IsSaveEnabled.Value = false;
                }

                HideTables();
            }            
        }
        private bool OpenFile(string aFilePath)
        {
            // Save current file name only if one file was selected
            _filePath = aFilePath;
            // Save file path to title
            UpdateTitle();

            // Parse xml files
            if (_docManager.LoadData(_filePath, _docType))
            {
                Components.Clear();
                GeneralGraphValues.Clear();

                // Update visual data
                UpdateData();

                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("Некорректный Формат файла!", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private bool Save()
        {
            _shouldSave = false;
            return string.IsNullOrEmpty(_filePath) ? false : _docManager.SaveData(_filePath);
        }

        private bool SavePreviousFile()
        {
            // Ask to save previous file if needed
            System.ComponentModel.CancelEventArgs e = new System.ComponentModel.CancelEventArgs(false);
            Closing(e);
            return !e.Cancel;
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

            if (_docType == DocType.Specification)
            {
                // Is add group button enabled
                IsAddEnabled.Value = _selectedItem.NodeType == NodeType.Group && _selectedItem.Name != Constants.DefaultGroupName;
                
                // Is remove group button enabled
                IsRemoveEnabled.Value = _selectedItem.NodeType == NodeType.SubGroup && _selectedItem.Name != Constants.DefaultGroupName;
            }
            else if (_docType == DocType.Bill)
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

            // Is specification table visible
            IsSpecificationTableVisible.Value = _docType == DocType.Specification && isGroup;
            // Is bill table visible
            IsBillTableVisible.Value = _docType == DocType.Bill && isGroup;

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
            return _project.GetGroupData(ConfigurationName, _docType, groupInfo);
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
            UpdateUndoRedoComponents();
        }

        private void UpdateConfiguration(Node aCollection, string aCfgName, IDictionary<string, Group> aGroups)
        {
            // Populate configuration tree
            Node treeItemCfg = new Node() { Name = aCfgName, NodeType = NodeType.Configuration, Parent = aCollection, Nodes = new ObservableCollection<Node>() };
            foreach (var grp in aGroups)
            {
                string groupName = grp.Key;
                if (string.IsNullOrEmpty(groupName))
                {
                    groupName = Constants.DefaultGroupName;
                }

                Node treeItemGroup = new Node() { Name = groupName, NodeType = NodeType.Group, Parent = treeItemCfg, Nodes = new ObservableCollection<Node>() };              
                foreach (var sub in grp.Value.SubGroups.AsNotNull())
                {
                    Node treeItemSubGroup = new Node() { Name = sub.Key, NodeType = NodeType.SubGroup, Parent = treeItemGroup };
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
            UpdateUndoRedoGraph();
        }

        private void UpdateGroups()
        {
            switch (_docType)
            {
                case DocType.Specification:
                    _specification.Nodes.Clear();
                    foreach (var cfg in _docManager.Project.Configurations)
                    {
                        UpdateConfiguration(_specification, cfg.Key, cfg.Value.Specification);
                    }
                    break;
                case DocType.Bill:
                    _bill.Nodes.Clear();
                    foreach (var cfg in _docManager.Project.Configurations)
                    {
                        UpdateConfiguration(_bill, cfg.Key, cfg.Value.Bill);
                    }
                    break;
            }            
        }

        private void UpdateUndoRedoMenu(UndoRedoStack<IList<object>> undoRedoStack)
        {
            IsUndoEnabled.Value = undoRedoStack.IsUndoEnabled;
            IsRedoEnabled.Value = undoRedoStack.IsRedoEnabled;
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
                        node.Nodes.InsertSorted(new MenuNode() { Name = doc.Key, Parent = node });
                    }
                    TableContextMenu.InsertSorted(node);
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
                        node.Nodes.InsertSorted(new MenuNode() { Name = doc.Key, Parent = node });
                    }
                    node.Nodes.InsertSorted(new MenuNode() { Name = Constants.NewMaterialMenuItem, Parent = node });
                    TableContextMenu.InsertSorted(node);
                }
                TableContextMenuEnabled.Value = true;
            }
        }

        private void UpdateTitle()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Title.Value = WindowTitle;
            }
            else
            {
                Title.Value = WindowTitle + " - " + Path.GetFileName(_filePath);
            }
        }

        private void UpdateUndoRedoComponents()
        {
            // Update undo / redo stack
            _undoRedoComponents.Add(Components.GetMementos());
            UpdateUndoRedoMenu(_undoRedoComponents);
        }

        private void UpdateUndoRedoGraph()
        {
            // Update undo / redo stack
            _undoRedoGraphs.Add(GeneralGraphValues.GetMementos());
            UpdateUndoRedoMenu(_undoRedoGraphs);
        }

        private void HideTables()
        {
            IsSpecificationTableVisible.Value = false;
            IsBillTableVisible.Value = false;
            IsGeneralGraphValuesVisible.Value = false;
        }

        private void UpdateDocType(DocType aType)
        {
            _docType = aType;
            IsExportExcelEnabled.Value = _excelManager.CanExport(_docType);
        }
    }
}
