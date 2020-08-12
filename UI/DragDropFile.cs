using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using GostDOC.Events;

namespace GostDOC.UI
{
    class DragDropFile : IDropTarget
    {
        public event EventHandler<TEventArgs<string>> FileDropped;

        private string GetFilePath(IDropInfo dropInfo)
        {
            var dataObject = dropInfo.Data as DataObject;
            if (dataObject != null && dataObject.ContainsFileDropList())
            {
                var dragFileList = dataObject.GetFileDropList();
                if (dragFileList.Count == 1)
                {
                    foreach (var file in dragFileList)
                    {
                        var extension = Path.GetExtension(file);
                        if (extension != null && extension.Equals(".xml"))
                        {
                            dropInfo.Effects = DragDropEffects.Copy;
                            return file;
                        }
                    }
                }
            }
            dropInfo.Effects = DragDropEffects.None;
            return string.Empty;
        }
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            GetFilePath(dropInfo);
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var filePath = GetFilePath(dropInfo);
            if (!string.IsNullOrEmpty(filePath))
            {
                FileDropped?.Invoke(this, new TEventArgs<string>(filePath));
            }
        }
    }
}
