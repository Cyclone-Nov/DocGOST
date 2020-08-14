using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static void CreateConfiguration()
        {
            NewFile dlg = new NewFile();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
            }
        }
    }
}
