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
    public static class CommonDialogs
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

        public static string SaveFileAs(string aFilter, string aTitle)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = aFilter;
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
    }
}
