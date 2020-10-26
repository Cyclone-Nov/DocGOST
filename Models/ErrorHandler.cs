using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Events;

namespace GostDOC.Models
{
    class ErrorHandler
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        #region Singleton
        private static readonly Lazy<ErrorHandler> _instance = new Lazy<ErrorHandler>(() => new ErrorHandler(), true);
        public static ErrorHandler Instance => _instance.Value;
        ErrorHandler()
        {
        }
        #endregion

        public event EventHandler<TEventArgs<string>> ErrorAdded;

        public void Error(string aError)
        {
            ErrorAdded?.Invoke(this, new TEventArgs<string>(aError));

            _log.Error(aError);
        }
    }
}
