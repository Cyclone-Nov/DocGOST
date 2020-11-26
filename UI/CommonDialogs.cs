using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GostDOC.Dictionaries;
using GostDOC.Models;
using GostDOC.Views;

namespace GostDOC.UI
{
    internal static class CommonDialogs
    {
        public static string GetGroupName()
        {
            NewGroup newGroupDlg = new NewGroup();
            var result = newGroupDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return newGroupDlg.GroupName.Text;
            }
            return null;
        }

        public static string EditGroupName(string aSource)
        {
            NewGroup newGroupDlg = new NewGroup();
            
            newGroupDlg.Title = "Изменить группу";
            newGroupDlg.GroupName.Text = aSource;

            var result = newGroupDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return newGroupDlg.GroupName.Text;
            }
            return null;
        }

        public static string GetMainFileName(string[] aFileNames)
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

        public static SubGroupInfo SelectGroup(IDictionary<string, IEnumerable<string>> aGroups)
        {
            SelectGroup dlg = new SelectGroup();
            dlg.ViewModel.SetGroups(aGroups);
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dlg.ViewModel.SubGroupInfo;
            }
            return null;
        }

        public static bool CreateConfiguration()
        {
            NewFile dlg = new NewFile();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return true;
            }
            return false;
        }

        public static string SaveFileAs(string aFilter, string aTitle, string aFilename = "")
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = aFilter;
            save.FileName = aFilename;
            save.Title = aTitle;
            if (save.ShowDialog() == DialogResult.OK)
            {
                return save.FileName;
            }
            return null;
        }

        public static string OpenFile(string aFilter, string aTitle)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = aFilter;
            open.Title = aTitle;

            if (open.ShowDialog() == DialogResult.OK)
            {
                return open.FileName;
            }
            return null;
        }

        public static Product AddProduct(ProductTypesDoc aDocType)
        {
            NewProduct newProductDlg = new NewProduct();
            switch (aDocType)
            {
                case ProductTypesDoc.Materials:
                    newProductDlg.Title = "Добавить материал";
                    newProductDlg.MaterialName.Text = "Новый материал";
                    break;
                case ProductTypesDoc.Others:
                case ProductTypesDoc.Standard:
                    newProductDlg.Title = "Добавить изделие";
                    newProductDlg.MaterialName.Text = "Новое изделие";
                    break;
            }

            var result = newProductDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return new Product()
                {
                    Name = newProductDlg.MaterialName.Text,
                    Note = newProductDlg.MaterialNote.Text
                };
            }
            return null;
        }

        public static Product UpdateProduct(Product aSource)
        {
            NewProduct NewProduct = new NewProduct();

            NewProduct.Title = "Изменить изделие";
            NewProduct.MaterialName.Text = aSource.Name;
            NewProduct.MaterialNote.Text = aSource.Note;

            var result = NewProduct.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return new Product()
                {
                    Name = NewProduct.MaterialName.Text,
                    Note = NewProduct.MaterialNote.Text
                };
            }
            return null;
        }

        public static void CreateEditProducts(ProductTypesDoc aDocType)
        {
            EditProducts dlg = new EditProducts();
            dlg.ViewModel.SetDocType(aDocType);
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
            }
        }

        public static void ShowLoadErrors(IList<string> aErrors)
        {
            LoadErrors dlg = new LoadErrors();
            dlg.ViewModel.SetErrors(aErrors);
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
            }
        }
    }
}
