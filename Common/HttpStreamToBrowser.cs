using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    class HttpDataToBrowser
    {
        public const string HostUri = "http://localhost:8088/PsuedoWebHost/";

        private HttpListener _httpListener = new HttpListener();

        #region Singleton
        private static readonly Lazy<HttpDataToBrowser> _instance = new Lazy<HttpDataToBrowser>(() => new HttpDataToBrowser(), true);
        public static HttpDataToBrowser Instance => _instance.Value;
        #endregion

        HttpDataToBrowser()
        {
            _httpListener.Prefixes.Add(HostUri);
            _httpListener.Start();
        }

        public void SetData(byte[] aData)
        {
            _httpListener.BeginGetContext((ar) => {

                HttpListenerContext context = _httpListener.EndGetContext(ar);

                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/pdf";

                // Construct a response.
                if (aData != null)
                {
                    response.ContentLength64 = aData.Length;
                    // Get a response stream and write the PDF to it.
                    Stream oStream = response.OutputStream;
                    oStream.Write(aData, 0, aData.Length);
                    oStream.Flush();
                }

                response.Close();
            }, null);
        }
        
    }
}
