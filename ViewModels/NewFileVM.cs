using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class NewFileVM
    {
        private DocManager _docManager = DocManager.Instance;

        public ObservableProperty<string> ProjectName { get; } = new ObservableProperty<string>("Новый проект");
        public ObservableProperty<ushort> ConfigurationCount { get; } = new ObservableProperty<ushort>(1);
        public ObservableCollection<GraphValueVM> GraphValues { get; } = new ObservableCollection<GraphValueVM>();
        public ICommand CreateNewCmd => new Command<Window>(CreateNew);

        public NewFileVM()
        {
            using (var reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, "DefaultGraphNames.txt")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    GraphValues.Add(new GraphValueVM(line, string.Empty));
                }
            }
        }

        private void CreateNew(Window wnd)
        {
            // Set prj name
            _docManager.Project.Name = ProjectName.Value;
            // Set configurations
            _docManager.Project.Configurations.Clear();
            for (int i = 0; i < ConfigurationCount.Value; i++)
            {
                Configuration cfg = new Configuration() { Name = $"-{i:00}" };
                cfg.FillDefaultGroups();

                if (i == 0)
                {
                    // Fill graphs for 1st configuration only
                    foreach (var graph in GraphValues)
                    {
                        cfg.Graphs.Add(graph.Name.Value, graph.Text.Value);
                    }
                    // Fill default groups
                    cfg.FillDefaultGraphs();
                }
                // Add configuration
                _docManager.Project.Configurations.Add(cfg.Name, cfg);
            }

            // Close current window
            wnd.DialogResult = true;
            wnd.Close();
        }
    }
}
