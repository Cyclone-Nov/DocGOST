using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        public static void CreateEditMaterials()
        {
            EditMaterials dlg = new EditMaterials();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
            }
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

        public static Material AddMaterial()
        {
            NewMaterial newMaterialDlg = new NewMaterial();
            var result = newMaterialDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return new Material()
                {
                    Name = newMaterialDlg.MaterialName.Text,
                    Note = newMaterialDlg.MaterialNote.Text
                };
            }
            return null;
        }

        public static Material UpdateMaterial(Material aSource)
        {
            NewMaterial newMaterialDlg = new NewMaterial();

            newMaterialDlg.Title = "Изменить материал";
            newMaterialDlg.MaterialName.Text = aSource.Name;
            newMaterialDlg.MaterialNote.Text = aSource.Note;

            var result = newMaterialDlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return new Material()
                {
                    Name = newMaterialDlg.MaterialName.Text,
                    Note = newMaterialDlg.MaterialNote.Text
                };
            }
            return null;
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
