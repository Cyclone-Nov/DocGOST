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
using SoftCircuits.Collections;
using GostDOC.Dictionaries;
using GostDOC.Context;

namespace GostDOC.ViewModels
{
    class MainWindowVM
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private Node _elements = new Node() { Name = "Перечень элементов", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _specification = new Node() { Name = "Спецификация", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _bill = new Node() { Name = "Ведомость покупных изделий", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _bill_D27 = new Node() { Name = "Ведомость комплектации", NodeType = NodeType.Root, Nodes = new ObservableCollection<Node>() };
        private Node _selectedItem = null;

        private DocType _docType = DocType.None;

        private string _filePath = null;
        private bool _shouldSave = false;

        private Dictionary<string, List<Tuple<string, int>>> _specifiactionPositionsDic = null;

        private DocManager _docManager = DocManager.Instance;

        private DocumentTypes _docTypes = DocManager.Instance.DocumentTypes;

        private ProjectWrapper _project = new ProjectWrapper();
        private List<MoveInfo> _moveInfo = new List<MoveInfo>();

        private UndoRedoStack<IList<object>> _undoRedoComponents = new UndoRedoStack<IList<object>>();
        private UndoRedoStack<IList<object>> _undoRedoGraphs = new UndoRedoStack<IList<object>>();

        private ExcelManager _excelManager = new ExcelManager();
        
        private ErrorHandler _loadError = ErrorHandler.Instance;
        private List<string> _loadErrors = new List<string>();

        private Progress _progress;
        private LogView _logView = null;

        private PurchaseDepartment _purchaseDepartment = new PurchaseDepartment();

        public ObservableProperty<string> Title { get; } = new ObservableProperty<string>();
        public ObservableProperty<Node> SelectedItem { get; } = new ObservableProperty<Node>();
        public ObservableProperty<bool> IsSpecificationTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsBillTableVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsD27TableVisible { get; } = new ObservableProperty<bool>(false);

        public ObservableProperty<bool> IsD27ProfileVisible { get; } = new ObservableProperty<bool>(false);        
        public ObservableProperty<ComponentVM> ComponentsSelectedItem { get; } = new ObservableProperty<ComponentVM>();
        public ObservableCollection<ComponentVM> Components { get; } = new ObservableCollection<ComponentVM>();
        // Graphs
        public ObservableProperty<bool> IsGeneralGraphValuesVisible { get; } = new ObservableProperty<bool>(false);
        public ObservableCollection<GraphValueVM> GeneralGraphValues { get; } = new ObservableCollection<GraphValueVM>();

        public ComponentSupplierProfileVM ComponentSupplierProfile { get; set; } = new ComponentSupplierProfileVM();

        public ProductSupplierProfileVM ProductSupplierProfile { get; set; } = new ProductSupplierProfileVM();

        //private ObservableCollection<ComponentSupplierProfileVM> ComponentsSupplierProfiles { get; } = new ObservableCollection<ComponentSupplierProfileVM>();

        public ObservableProperty<string> ComponentPropertiesHeader { get; } = new ObservableProperty<string>();

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
        public ObservableProperty<bool> IsSaveAsEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsExportExcelEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsExportPdfEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsAddComponentEnabled { get; } = new ObservableProperty<bool>(true);
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
        public ICommand SaveFileAsCmd => new Command(SaveFileAs, IsSaveEnabled);
        public ICommand ClosingCmd => new Command<System.ComponentModel.CancelEventArgs>(Closing);
        public ICommand AddComponentCmd => new Command(AddComponent, IsAddComponentEnabled);
        public ICommand AddEmptyRowCmd => new Command(AddEmptyRow);
        public ICommand RemoveComponentsCmd => new Command<IList<object>>(RemoveComponents);
        public ICommand MoveComponentsCmd => new Command<IList<object>>(MoveComponents);
        public ICommand TreeViewSelectionChangedCmd => new Command<Node>(TreeViewSelectionChanged);
        public ICommand AddGroupCmd => new Command(AddGroup);
        public ICommand RemoveGroupCmd => new Command(RemoveGroup);
        public ICommand UpComponentsCmd => new Command<IList<object>>(UpComponents);
        public ICommand DownComponentsCmd => new Command<IList<object>>(DownComponents);
        public ICommand UpdatePdfCmd => new Command(UpdatePdf, IsExportPdfEnabled);
        public ICommand ClickMenuCmd => new Command<MenuNode>(ClickMenu);
        public ICommand EditNameValueCmd => new Command(EditNameValue);
        public ICommand EditComponentsCmd => new Command<DataGrid>(EditComponents);
        public ICommand DataGridMouseButtonDownCmd => new Command<DataGrid>(DataGridMouseButtonDown);
        public ICommand DataGridBeginningEditCmd => new Command<DataGridBeginningEditEventArgs>(DataGridBeginningEdit);
        public ICommand ExportPDFCmd => new Command(ExportPDF, IsExportPdfEnabled);
        public ICommand ExportExcelCmd => new Command(ExportExcel, IsExportExcelEnabled);

        public ICommand SpecPositionRecalcCmd => new Command(SpecificationPositionRecalc);
        

        #region Dictionaries
        public ICommand ImportMaterialsCmd => new Command(ImportMaterials);
        public ICommand SaveMaterialsCmd => new Command(SaveMaterials);
        public ICommand UpdateMaterialsCmd => new Command(UpdateMaterials);
        public ICommand ImportOthersCmd => new Command(ImportOthers);
        public ICommand SaveOthersCmd => new Command(SaveOthers);
        public ICommand UpdateOthersCmd => new Command(UpdateOthers);
        public ICommand ImportStandardCmd => new Command(ImportStandard);
        public ICommand SaveStandardCmd => new Command(SaveStandard);
        public ICommand UpdateStandardCmd => new Command(UpdateStandard);
        #endregion Dictionaries

        public ICommand CopyCellCmd => new Command<DataGridCellInfo>(CopyCell);
        public ICommand PasteCellCmd => new Command<DataGridCellInfo>(PasteCell);
        public ICommand AutoSortCheckedCmd => new Command<bool>(AutoSortChecked);
        public ICommand ShowLogCmd => new Command(ShowLog);

        public string WindowTitle
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return "ПО формирования документов КД на изделие в соответствие с ГОСТ " + version;
            }
        }
        /// <summary>
        /// Current selected configuration
        /// </summary>
        public string ConfigurationName
        {
            get
            {
                var item = _selectedItem;
                if (_selectedItem?.NodeType == NodeType.Component)
                {
                    item = _selectedItem.Parent;
                }
                if (item?.NodeType == NodeType.Configuration)
                {
                    return item.Name;
                }
                if (item?.NodeType == NodeType.Group)
                {
                    return item.Parent.Name;
                }
                if (item?.NodeType == NodeType.SubGroup)
                {
                    return item.Parent.Parent.Name;
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
            // Subscribe to load errors
            _loadError.ErrorAdded += OnLoadError;
            // Subscribe to export complete event
            _excelManager.ExportComplete += OnExportComplete;
            // Update title
            UpdateTitle();

            // Init PD
            _purchaseDepartment.ConnectToDB("Database");
        }

        #region Commands impl

        private void CopyCell(DataGridCellInfo cellInfo)
        {
            var txt = (cellInfo.Column?.GetCellContent(cellInfo.Item) as TextBlock)?.Text;
            if (txt != null)
            {
                Clipboard.SetText(txt);
                cellInfo.Column.OnCopyingCellClipboardContent(cellInfo.Item);
            }
        }

        private void PasteCell(DataGridCellInfo cellInfo)
        {
            var txtBlock = (cellInfo.Column?.GetCellContent(cellInfo.Item) as TextBlock);
            if (txtBlock != null)
            {
                txtBlock.Text = Clipboard.GetText();
                cellInfo.Column.OnPastingCellClipboardContent(cellInfo.Item, txtBlock.Text);
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
        private void EditNameValue(object obj)
        {
            UpdateUndoRedoGraph();
            IsUndoEnabled.Value = true;
            _shouldSave = true;
        }

        private bool locker = true;
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
                    _shouldSave = true;
                }
                finally
                {
                    locker = true;
                }
            }
        }

        private void DataGridMouseButtonDown(DataGrid obj)
        {
            bool commit = obj.CommitEdit(DataGridEditingUnit.Row, true);
            if (!commit)
            {
                commit = obj.CancelEdit(DataGridEditingUnit.Row);
            }
            if (commit)
            {
                obj.UnselectAllCells();
                obj.Items.Refresh();
            }
        }

        private void DataGridBeginningEdit(DataGridBeginningEditEventArgs e)
        {
            if (ComponentsSelectedItem?.Value.IsReadOnly == true)
            {
                e.Cancel = true;
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
                _docManager.Reset();

                ClearVisible();

                DocNodes.Clear();
                DocNodes.Add(_specification);
                
                _filePath = string.Empty;

                UpdateDocType(DocType.Specification);
                UpdateTitle();
                UpdateData();
                HideTables();

                _shouldSave = true;
                IsSaveEnabled.Value = true;
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
            var path = CommonDialogs.SaveFileAs("xml Files *.xml | *.xml", "Сохранить файл", GetDefaultFileName("xml", false));
            if (!string.IsNullOrEmpty(path))
            {
                _filePath = path;
                // Save file path to title
                UpdateTitle();
                // Save file
                Save();
            }
        }

        private void SaveData()
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.NodeType == NodeType.Group || _selectedItem.NodeType == NodeType.SubGroup)
                {
                    SaveComponents();
                }
                else if (_selectedItem.NodeType == NodeType.Root)
                {
                    SaveGraphValues();
                }
            }
        }

        private void TreeViewSelectionChanged(Node obj)
        {
            SaveData();

            _selectedItem = obj;

            if (_selectedItem != null)
            {
                // Update current table
                UpdateSelectedDocument();
            }            
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
            var cmp = new ComponentVM();
            cmp.Name.Value = Constants.ComponentName;
            cmp.WhereIncluded.Value = _project.GetGraphValue(ConfigurationName, Constants.GraphSign);
            AddComponent(cmp);
        }

        private void AddEmptyRow(object obj)
        {
            var cmp = new ComponentVM();
            cmp.CountDev.Value = 0;
            cmp.IsReadOnly = true;
            Components.Add(cmp);
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
                        Component component = new Component(cmp.Guid, cmp.Count.Value);
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

        private void SaveComponents()
        {
            string cfgName = ConfigurationName;

            // Update components properties
            List<Component> components = new List<Component>();
            foreach (var cmp in Components)
            {
                Component component = new Component(cmp.Guid, cmp.Count.Value);
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
        }

        private void SaveGraphValues()
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

            Node newGroup = null;

            // Add group or subgroup
            if (_selectedItem.NodeType == NodeType.Configuration)
            {
                _project.AddGroup(_selectedItem.Name, _docType, new SubGroupInfo(name, null));
                newGroup = new Node() { Name = name, NodeType = NodeType.Group, Parent = _selectedItem, Nodes = new ObservableCollection<Node>() };
            }
            else if (_selectedItem.NodeType == NodeType.Group)
            {
                _project.AddGroup(_selectedItem.Parent.Name, _docType, new SubGroupInfo(_selectedItem.Name, name));
                newGroup = new Node() { Name = name, NodeType = NodeType.SubGroup, Parent = _selectedItem, Nodes = new ObservableCollection<Node>() };
            }

            if (newGroup != null)
            {
                SelectedItem.Value.Nodes.Add(newGroup);
                SelectedItem.Value = newGroup;
            }

            // Should save
            _shouldSave = true;
        }

        private void RemoveGroup(object obj)
        {
            SaveData();

            bool removeComponents = false;
            if (_docType == DocType.Specification)
            {
                // Ask user to remove components in group
                var groupData = GetGroupData();
                if (groupData?.Components.Count > 0)
                {
                    var result = System.Windows.MessageBox.Show("Удалить также и компоненты в выбранной группе?\r\n\r\nДа\t - компоненты будут удалены безвозвратно\r\nНет\t - компоненты будут перенесены в раздел\r\nОтмена\t - еще подумаю, ничего не делать", "Удаление группы", MessageBoxButton.YesNoCancel);
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

                var groupToRemove = _selectedItem;
                SelectedItem.Value = _selectedItem.Parent;
                SelectedItem.Value.Nodes.Remove(groupToRemove);
            }

            // Should save
            _shouldSave = true;
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
            SaveData();

            if (_docManager.PrepareData(_docType))
            {
                _docManager.PreparePDF(_docType);
                CurrentPdfData.Value = _docManager.GetPdfData(_docType);
            }

        }

        private Tuple<string, string> GetGroups(MenuNode obj)
        {
            if (obj.Parent == null)
            {
                return new Tuple<string, string>(null, null);
            }
            else if (obj.Parent?.Parent != null)
            {
                return new Tuple<string, string>(obj.Parent.Parent.Name, obj.Parent.Name);
            }
            else
            {
                return new Tuple<string, string>(obj.Parent.Name, null);
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
                    var included = _project.GetGraphValue(ConfigurationName, Constants.GraphSign);

                    ComponentVM cmp = new ComponentVM();
                    cmp.Name.Value = doc.Name;
                    cmp.Sign.Value = included + doc.Code;
                    cmp.WhereIncluded.Value = included;
                    cmp.Format.Value = "А4";
                    AddComponent(cmp);
                }        
            }
            else if (GroupName.Equals(Constants.GroupMaterials))
            {
                ClickMenu(obj, DocManager.Instance.Materials, Constants.NewMaterialMenuItem);
            }
            else if (GroupName.Equals(Constants.GroupOthers))
            {
                ClickMenu(obj, DocManager.Instance.Others, Constants.NewProductMenuItem);
            }
            else if (GroupName.Equals(Constants.GroupStandard))
            {
                ClickMenu(obj, DocManager.Instance.Standard, Constants.NewProductMenuItem);
            }
        }

        private void ClickMenu(MenuNode aNode, ProductTypes aProductTypes, string aNewItemName)
        {
            Product product;
            var groups = GetGroups(aNode);
            if (aNode.Name == aNewItemName)
            {
                // Add material
                product = CommonDialogs.AddProduct(aProductTypes.DocType);
                if (product != null)
                {
                    if (aProductTypes.AddProduct(groups.Item1, groups.Item2, product))
                    {
                        // Add node
                        var node = new MenuNode() { Name = product.Name, Parent = aNode.Parent };
                        if (aNode.Parent != null)
                        {
                            aNode.Parent.Nodes.InsertSorted(node);
                        }
                        else
                        {
                            TableContextMenu.InsertSorted(node);
                        }
                    }
                }
            }
            else if (aNode.Name == Constants.NewGroupMenuItem)
            {
                var name = CommonDialogs.GetGroupName();
                if (!string.IsNullOrEmpty(name))
                {
                    // Cerate new menu item
                    var node = new MenuNode() { Name = name, Parent = aNode.Parent, Nodes = new ObservableCollection<MenuNode>() };
                    // Add new product menu item
                    node.Nodes.InsertSorted(new MenuNode() { Name = aNewItemName, Parent = node });

                    if (string.IsNullOrEmpty(groups.Item1))
                    {
                        if (aProductTypes.AddGroup(name))
                        {
                            // Add new group menu item
                            node.Nodes.InsertSorted(new MenuNode() { Name = Constants.NewGroupMenuItem, Parent = node });
                            TableContextMenu.InsertSorted(node);
                        }
                    } 
                    else
                    {
                        if (aProductTypes.AddSubGroup(groups.Item1, name))
                        {
                            aNode.Parent.Nodes.InsertSorted(node);
                        }
                    }
                }
                // Save file
                aProductTypes.Save();
                return;
            }
            else
            {
                // Find material
                product = aProductTypes.GetProduct(groups.Item1, groups.Item2, aNode.Name);
            }

            // Add product to components
            if (product != null)
            {
                ComponentVM cmp = new ComponentVM();
                cmp.Name.Value = product.Name;
                cmp.Note.Value = product.Note;
                cmp.WhereIncluded.Value = _project.GetGraphValue(ConfigurationName, Constants.GraphSign);
                if (GroupName == Constants.GroupMaterials)
                {
                    cmp.MaterialGroup = groups.Item1;
                }
                AddComponent(cmp);
            }

            // Save file
            aProductTypes.Save();
        }

        private void ExportPDF(object obj)
        {
            if (IsExportPdfEnabled.Value)
            {
                var path = CommonDialogs.SaveFileAs("PDF files (*.pdf) | *.pdf", "Сохранить файл", GetDefaultFileName("pdf", true));
                if (!string.IsNullOrEmpty(path))
                {
                    _docManager.SavePDF(_docType, path);
                }
            }
        }

        private void ExportExcel(object obj)
        {
            if (_excelManager.CanExport(_docType))
            {
                var path = CommonDialogs.SaveFileAs("Excel Files *.xlsx | *.xlsx", "Сохранить файл", GetDefaultFileName("xlsx", true));
                if (!string.IsNullOrEmpty(path))
                {
                    _progress = new Progress();
                    _excelManager.Export(_docType, path);
                    _progress.ShowDialog();
                }
            }
        }

        private void ImportMaterials(object obj)
        {
            var path = CommonDialogs.OpenFile("XML Files *.xml | *.xml", "Открыть файл материалов");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Materials.Import(path);
            }
        }

        private void SaveMaterials(object obj)
        {
            var path = CommonDialogs.SaveFileAs("XML Files *.xml | *.xml", "Сохранить файл материалов");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Materials.Save(path);
            }
        }

        private void UpdateMaterials(object obj)
        {
            CommonDialogs.CreateEditProducts(ProductTypesDoc.Materials);
            if (GroupName.Equals(Constants.GroupMaterials))
            {
                UpdateTableContextMenu();
            }
        }

        private void ImportOthers(object obj)
        {
            var path = CommonDialogs.OpenFile("XML Files *.xml | *.xml", "Открыть файл прочих изделий");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Others.Import(path);
            }
        }

        private void SaveOthers(object obj)
        {
            var path = CommonDialogs.SaveFileAs("XML Files *.xml | *.xml", "Сохранить файл прочих изделий");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Others.Save(path);
            }
        }

        private void UpdateOthers(object obj)
        {
            CommonDialogs.CreateEditProducts(ProductTypesDoc.Others);
            if (GroupName.Equals(Constants.GroupOthers))
            {
                UpdateTableContextMenu();
            }
        }

        private void ImportStandard(object obj)
        {
            var path = CommonDialogs.OpenFile("XML Files *.xml | *.xml", "Открыть файл стандартных изделий");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Standard.Import(path);
            }
        }

        private void SaveStandard(object obj)
        {
            var path = CommonDialogs.SaveFileAs("XML Files *.xml | *.xml", "Сохранить файл стандартных изделий");
            if (!string.IsNullOrEmpty(path))
            {
                DocManager.Instance.Standard.Save(path);
            }
        }

        private void UpdateStandard(object obj)
        {
            CommonDialogs.CreateEditProducts(ProductTypesDoc.Standard);
            if (GroupName.Equals(Constants.GroupStandard))
            {
                UpdateTableContextMenu();
            }
        }

        private void AutoSortChecked(bool aChecked)
        {
            if (aChecked)
            {
                SaveComponents();
                UpdateGroup();
            }
        }

        private void ShowLog(object obj)
        {
            if (_logView == null || _logView.IsClosed)
            {
                _logView = new LogView();
                _logView.Show();
            }
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

                DocNodes.Clear();

                if (OpenFile(path))
                {
                    DocNodes.Add(aNode);
                    IsSaveEnabled.Value = (aDocType == DocType.Specification || aDocType == DocType.Bill);                    
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

            ClearVisible();

            // Parse xml files
            switch (_docManager.LoadData(_filePath, _docType))
            {
                case OpenFileResult.Ok:
                    // Update visual data
                    UpdateData();
                    // Show errors
                    ShowErrors();
                    // Success
                    return true;
                case OpenFileResult.FileFormatError:
                    System.Windows.MessageBox.Show("Попытка открыть файл ведомости в другом режиме!", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case OpenFileResult.Fail:
                    System.Windows.MessageBox.Show("Некорректный Формат файла!", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
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
            bool isAddComponentEnabled = true;
             // Is group or subgroup selected
            bool isGroup = _selectedItem.NodeType == NodeType.Group || _selectedItem.NodeType == NodeType.SubGroup;
            // Is graph table visible
            IsGeneralGraphValuesVisible.Value = _selectedItem.NodeType == NodeType.Root;
            if (IsGeneralGraphValuesVisible.Value)
            {
                UpdateGraphValues();
            }

            if (_docType == DocType.Specification)
            {
                // Is add group button enabled
                IsAddEnabled.Value = _selectedItem.NodeType == NodeType.Group && _selectedItem.Name != Constants.DefaultGroupName;
                // Is add component enabled
                isAddComponentEnabled = GroupName != Constants.GroupMaterials;
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
            // Is add component enabled
            IsAddComponentEnabled.Value = isAddComponentEnabled;

            if (_docType == DocType.D27)
            {
                if (_selectedItem.NodeType == NodeType.Component)
                {
                    // Is D27 visible                    
                    IsD27ProfileVisible.Value = false;
                    IsD27TableVisible.Value = true;
                    UpdateComponentSuppliersProfile(SelectedItem.Value.Name);
                } else
                {                    
                    IsD27TableVisible.Value = false;
                    IsD27ProfileVisible.Value = true;
                    UpdateProductSupplierProfile();
                }
            }  
            else
            {
                IsD27TableVisible.Value = false;
                IsD27ProfileVisible.Value = false;
            }

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

        private void UpdateGroup(bool? aAutoSort = null)
        {
            var groupData = GetGroupData();
            if (groupData == null)
            {
                return;
            }

            if (aAutoSort == null)
                IsAutoSortEnabled.Value = groupData.AutoSort;
            else
            {
                IsAutoSortEnabled.Value = groupData.AutoSort = (bool)aAutoSort;                
            }

            // Fill components
            Components.Clear();
                        
            var positions = GetSpecificationPositions();
            int lastPosition = 0;
            foreach (var component in groupData.Components)
            {
                lastPosition = SetSpecificationPosition(positions, component, lastPosition);

                //add position here
                Components.Add(new ComponentVM(component));
            }
        }

        private void UpdateGroupData()
        {
            UpdateGroup();

            // Add initial value to undo / redo stack
            _undoRedoComponents.Clear();
            UpdateUndoRedoComponents();
        }

        private void UpdateConfiguration(Node aCollection, Group aGroup)
        {
            // Populate configuration tree
            foreach (var cmp in aGroup.Components)
            {
                Node node = new Node() { Name = cmp.GetProperty(Constants.ComponentName), NodeType = NodeType.Component, Parent = aCollection };
                aCollection.Nodes.Add(node);
            }

            foreach (var gp in aGroup.SubGroups.AsNotNull())
            {
                UpdateConfiguration(aCollection, gp.Value);
            }
        }

        private void UpdateConfiguration(Node aCollection, string aCfgName, Group aGroup)
        {
            // Populate configuration tree
            Node treeItemCfg = new Node() { Name = aCfgName, NodeType = NodeType.Configuration, Parent = aCollection, Nodes = new ObservableCollection<Node>() };
            UpdateConfiguration(treeItemCfg, aGroup);
            aCollection.Nodes.Add(treeItemCfg);
        }

        private void UpdateConfiguration(Node aCollection, string aCfgName, IDictionary<string, Group> aGroups)
        {
            // Populate configuration tree
            Node treeItemCfg = new Node() { Name = aCfgName, NodeType = NodeType.Configuration, Parent = aCollection, Nodes = new ObservableCollection<Node>() };
            UpdateGroups(treeItemCfg, aGroups);
            aCollection.Nodes.Add(treeItemCfg);
        }

        private void UpdateGroups(Node aTreeItemCfg, IDictionary<string, Group> aGroups)
        {
            // Populate configuration tree            
            foreach (var grp in aGroups)
            {
                //string groupName = grp.Name;
                string groupName = grp.Key;
                if (string.IsNullOrEmpty(groupName))
                {
                    groupName = Constants.DefaultGroupName;
                }

                Node treeItemGroup = new Node() { Name = groupName, NodeType = NodeType.Group, Parent = aTreeItemCfg, Nodes = new ObservableCollection<Node>() };
                foreach (var sub in grp.Value.SubGroups.AsNotNull())
                {
                    Node treeItemSubGroup = new Node() { Name = sub.Key, NodeType = NodeType.SubGroup, Parent = treeItemGroup };
                    treeItemGroup.Nodes.Add(treeItemSubGroup);
                }
                aTreeItemCfg.Nodes.Add(treeItemGroup);
            }            
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
                case DocType.D27:
                    _bill_D27.Nodes.Clear();
                    foreach (var cfg in _docManager.Project.Configurations)
                    {
                        UpdateConfiguration(_bill_D27, cfg.Key, cfg.Value.D27);
                    }
                    break;
            }            
        }

        private void UpdateUndoRedoMenu(UndoRedoStack<IList<object>> undoRedoStack)
        {
            IsUndoEnabled.Value = undoRedoStack.IsUndoEnabled;
            IsRedoEnabled.Value = undoRedoStack.IsRedoEnabled;

            _shouldSave = _shouldSave || undoRedoStack.IsUndoEnabled || undoRedoStack.IsRedoEnabled;
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
            aDst.Properties.Add(Constants.ComponentCount, aSrc.Count.Value.ToString());
            aDst.Properties.Add(Constants.ComponentNote, aSrc.Note.Value);
            aDst.Properties.Add(Constants.ComponentZone, aSrc.Zone.Value);
            aDst.Properties.Add(Constants.ComponentPosition, aSrc.Position.Value.ToString());
            aDst.Properties.Add(Constants.ComponentWhereIncluded, aSrc.WhereIncluded.Value);

            if (!string.IsNullOrEmpty(aSrc.MaterialGroup))
            {
                aDst.Properties.Add(Constants.ComponentMaterialGroup, aSrc.MaterialGroup);
            }
        }

        private void AddMenuItem(MenuNode aNode, ProductGroup aGroup, string aNewElement)
        {
            foreach (var product in aGroup.ProductsList)
            {
                aNode.Nodes.InsertSorted(new MenuNode() { Name = product.Key, Parent = aNode });
            }
            // Add product item
            aNode.Nodes.InsertSorted(new MenuNode() { Name = aNewElement, Parent = aNode });

            if (aNode.Parent == null)
            {
                // Add group button, only to groups
                aNode.Nodes.InsertSorted(new MenuNode() { Name = Constants.NewGroupMenuItem, Parent = aNode });
            }

            if (aGroup.SubGroups != null)
            {
                foreach (var gp in aGroup.SubGroups)
                {
                    MenuNode node = new MenuNode() { Name = gp.Key, Parent = aNode, Nodes = new ObservableCollection<MenuNode>() };
                    AddMenuItem(node, gp.Value, aNewElement);
                    aNode.Nodes.InsertSorted(node);
                }
            }
        }

        private void UpdateTableContextMenu(ProductTypes aProductTypes)
        {
            // Add groups
            foreach (var kvp in aProductTypes.Products.Groups)
            {
                MenuNode node = new MenuNode() { Name = kvp.Key, Nodes = new ObservableCollection<MenuNode>() };
                AddMenuItem(node, kvp.Value, Constants.NewProductMenuItem);
                TableContextMenu.InsertSorted(node);
            }
            // Add products
            foreach (var product in aProductTypes.Products.ProductsList)
            {
                TableContextMenu.InsertSorted(new MenuNode() { Name = product.Key });
            }
            // Add "new product" button
            TableContextMenu.InsertSorted(new MenuNode() { Name = Constants.NewProductMenuItem });
            TableContextMenu.InsertSorted(new MenuNode() { Name = Constants.NewGroupMenuItem });
            TableContextMenuEnabled.Value = true;
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
                // Add groups only
                foreach (var kvp in DocManager.Instance.Materials.Products.Groups)
                {
                    MenuNode node = new MenuNode() { Name = kvp.Key, Nodes = new ObservableCollection<MenuNode>() };
                    AddMenuItem(node, kvp.Value, Constants.NewMaterialMenuItem);
                    TableContextMenu.InsertSorted(node);
                }
                TableContextMenuEnabled.Value = true;
            }
            else if (GroupName.Equals(Constants.GroupOthers))
            {
                UpdateTableContextMenu(DocManager.Instance.Others);
            }
            else if (GroupName.Equals(Constants.GroupStandard))
            {
                UpdateTableContextMenu(DocManager.Instance.Standard);
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
            IsD27TableVisible.Value = false;
        }
        private void UpdateDocType(DocType aType)
        {
            _docType = aType;
            IsExportExcelEnabled.Value = _excelManager.CanExport(_docType);
            IsExportPdfEnabled.Value = _docType == DocType.Specification || _docType == DocType.Bill || _docType == DocType.ItemsList;
        }
        private void OnLoadError(object sender, TEventArgs<string> e)
        {
            _loadErrors.Add(e.Arg);
        }
        private void ShowErrors()
        {
            if (_loadErrors.Count > 0)
            {
                CommonDialogs.ShowLoadErrors(_loadErrors);
                _loadErrors.Clear();
            }
        }
        private void OnExportComplete(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (_progress != null)
                {
                    _progress.Close();
                    _progress = null;
                }
            }); 
        }

        /// <summary>
        /// пересчитать позицию для спецификации
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void SpecificationPositionRecalc(object sender)
        {
            if(_docType == DocType.Specification)
            {
                if (_docManager.PrepareData(_docType))
                {
                    var dataProperties = _docManager.GetPreparedDataProperties(_docType);
                    if (dataProperties.TryGetValue(Constants.AppDataSpecPositions, out var positions))
                    {
                        if (positions != null && positions is Dictionary<string, List<Tuple<string, int>>>)
                        {
                            _specifiactionPositionsDic = ((Dictionary<string, List<Tuple<string, int>>>)positions);
                            UpdateGroup(false);
                        }
                    }
                }
            }
        }


        #region  ========= PURCHASE DEPARTMENT ====================

        private void UpdateComponentSuppliersProfile(string aComponentName)
        {            
            var groupData = GetGroupData();            
            var components = groupData?.Components.Where(cmp => string.Equals(cmp.GetProperty(Constants.ComponentName), aComponentName));
            string doc = string.Empty;
            if (components?.Count() > 0)
            {
                var component = components.First();
                doc = component.GetProperty(Constants.ComponentDoc);
                if (string.Equals(aComponentName, doc))
                    doc = string.Empty;

                FillComponentSupplierProfile(component, aComponentName);
            }
            ComponentPropertiesHeader.Value = $"Свойства компонента {aComponentName} {doc}";
        }

        private void UpdateProductSupplierProfile()
        {
            ProductSupplierProfile.Quantity.Value = 2;
            ProductSupplierProfile.Note.Value = "Какое-то описание";
        }

        private void FillComponentSupplierProfile(Component aComponent, string aComponentName)
        {            
            var manufacturer = aComponent.GetProperty(Constants.ComponentSupplier);
            var quantity = aComponent.Count;// .GetProperty(Constants.ComponentSupplier);
            var quantity2 = aComponent.GetProperty(Constants.ComponentCount);
            var allQuantity = quantity* ProductSupplierProfile.Quantity.Value;

            ComponentSupplierProfile.Properties.Manufacturer.Value = manufacturer;
            ComponentSupplierProfile.Properties.Quantity.Value = (int)quantity;
            ComponentSupplierProfile.Properties.AllQuantity.Value = (int)allQuantity;

            _purchaseDepartment.ComponentName = aComponentName;
            _purchaseDepartment.GetComponentSupplierProfile();
        }

        #endregion PURCHASE DEPARTMENT

        private Tuple<string, string> GetSpecificationPositionDicKey()
        {
            //string subgroup_name = string.Empty;
            string group_name = string.Empty;
            string config_name = string.Empty;
            var item = _selectedItem;
            if (item.NodeType == NodeType.Component)
            {
                item = _selectedItem.Parent;
            }

            if (item.NodeType == NodeType.SubGroup)
            {
                //subgroup_name = item.Name;
                item = item.Parent;
            }

            if (item.NodeType == NodeType.Group)
            {
                group_name = item.Name;
                item = item.Parent;
            }

            if (item.NodeType == NodeType.Configuration)
            {
                config_name = item.Name;                
            }
                        
            return new Tuple<string, string>(($"{config_name} {group_name}").Trim(), group_name);
        }

        /// <summary>
        /// получить список позиций для компонентов для документа спецификация
        /// </summary>
        /// <returns></returns>
        private List<Tuple<string, int>> GetSpecificationPositions()
        {
            if (_specifiactionPositionsDic != null)
            {
                List<Tuple<string, int>> positions = null;
                var key = GetSpecificationPositionDicKey();
                if (_specifiactionPositionsDic.ContainsKey(key.Item1))
                    positions = _specifiactionPositionsDic[key.Item1];                
                return positions;
            }
            return null;
        }

        /// <summary>
        /// записать позицию в свойство Позиция для данного компонента
        /// </summary>
        /// <param name="aPositions"></param>
        /// <param name="aComponent"></param>
        private int SetSpecificationPosition(List<Tuple<string, int>> aPositions, Component aComponent, int aPrevPosition)
        {
            int retposition = 0;
            if (aPositions != null)
            {
                string name = aComponent.GetProperty(Constants.ComponentName);
                string designator = aComponent.GetProperty(Constants.ComponentSign);
                string val = ($"{name} {designator}").Trim();
                var positions = aPositions.Where(item => string.Equals(item.Item1, val));
                if (positions != null && positions.Count() > 0)
                {                    
                    if (positions.Count() == 1)
                    {
                        retposition = positions.First().Item2;
                        aComponent.SetPropertyValue(Constants.ComponentPosition, retposition.ToString());
                    } else
                    {
                        foreach (var pos in positions)
                        {
                            retposition = pos.Item2;
                            if (retposition != 0 && (retposition - aPrevPosition == 1))
                            {
                                aComponent.SetPropertyValue(Constants.ComponentPosition, retposition.ToString());
                                return retposition;
                            }
                        }
                    }
                }
            }
            return retposition;
        }


        private string GetDefaultFileName(string aExtension, bool aForExport)
        {
            string filename = string.Empty;
            if (!string.IsNullOrEmpty(_filePath))
            {
                if(aForExport)
                    filename = $"{Path.GetFileNameWithoutExtension(_filePath)}{_docManager.GetDocumentName(_docType)}.{aExtension}";
                else
                    filename = $"{Path.GetFileNameWithoutExtension(_filePath)}.{aExtension}";
            }
            else
            {
                //FirstOrDefault
                //var val = GeneralGraphValues.Where(k => string.Equals(k.Name.Value, "Обозначение")).ToArray();
                var val = GeneralGraphValues.First(k => string.Equals(k.Name.Value, "Обозначение"));
                if (val != null && !string.IsNullOrEmpty(val.Text.Value))
                {
                    if (aForExport)
                        filename = $"{val.Text.Value}{_docManager.GetDocumentName(_docType)}.{aExtension}";
                    else
                        filename = $"{val.Text.Value}.{aExtension}";
                }
            }
            return filename;
        }

        private void ClearVisible()
        {
            GeneralGraphValues.Clear();
            Components.Clear();
        }

        private void AddComponent(ComponentVM aComponent)
        {
            Components.Add(aComponent);
            UpdateUndoRedoComponents();

            if (IsAutoSortEnabled.Value)
            {
                SaveComponents();
                UpdateGroup();
            }
        }
    }
}
