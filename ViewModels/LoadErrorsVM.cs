using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class LoadErrorsVM
    {
        public ObservableCollection<string> Errors { get; } = new ObservableCollection<string>();

        public LoadErrorsVM()
        {
        }

        public void SetErrors(IList<string> aErrors)
        {
            Errors.AddRange(aErrors);
        } 
    }
}
