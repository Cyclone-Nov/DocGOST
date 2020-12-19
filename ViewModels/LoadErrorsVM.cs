using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GostDOC.Models;
using GostDOC.UI;

namespace GostDOC.ViewModels
{
    class LoadErrorsVM
    {
        public ObservableCollection<string> Errors { get; } = new ObservableCollection<string>();

        public ICommand SaveLogCmd => new Command(SaveLogFile);
        private string _filePath = string.Empty;

        public LoadErrorsVM()
        {
        }

        public void SetErrors(IList<string> aErrors)
        {
            Errors.AddRange(aErrors);
        }

        private void SaveLogFile(object obj = null)
        {
            string fileName = $"Log_{DateTime.Now.ToString("yyyy-MM-dd")}.txt";
            var path = CommonDialogs.SaveFileAs("txt Files *.txt | *.txt", "Сохранить файл", fileName);
            if (!string.IsNullOrEmpty(path))
            {
                _filePath = path;

                // Save file
                try
                {
                    Save(_filePath);
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show("Не удалось сохранить файл!", $"Ошибка: {ex.Message}", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void Save(string aFileName)
        {   
            System.IO.File.WriteAllLines(aFileName, Errors);            
        }
        
    }
}
