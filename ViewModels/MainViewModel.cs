using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.ViewModels.ElementsList;
using GostDOC.ViewModels.Specification;

namespace GostDOC.ViewModels
{
    class MainViewModel
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public ObservableCollection<SpecificationEntryVM> SpecificationTable { get; set; }

        public MainViewModel()
        {
            SpecificationTable = new ObservableCollection<SpecificationEntryVM>();

            for (int i = 0; i < 20; ++i)
            {
                var s = new SpecificationEntryVM();
                s.Name.Value = RandomString(10);
                s.Designation.Value = RandomString(3);
                s.Format.Value = RandomString(6);
                s.Note.Value = RandomString(30);
                s.Quantity.Value = (uint)Math.Abs(random.Next());
                s.Zone.Value = RandomString(4);

                SpecificationTable.Add(s);
            }
        }

    }
}
