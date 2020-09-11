using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    class HttpDataToBrowser
    {
        private HttpListener _httpListener = new HttpListener();

        #region Singleton
        private static readonly Lazy<HttpDataToBrowser> _instance = new Lazy<HttpDataToBrowser>(() => new HttpDataToBrowser(), true);
        public static HttpDataToBrowser Instance => _instance.Value;
        #endregion

        public string HostUri { get; private set; }

        HttpDataToBrowser()
        {
            for (int port = 40000; port < 45000; port++)
            {
                if (!IsPortBisy(port))
                {
                    HostUri = $"http://localhost:{port}/pdf/";
                    _httpListener.Prefixes.Add(HostUri);
                    _httpListener.Start();
                    break;
                }
            }
        }

        private bool IsPortBisy(int port)
        {
            System.Net.IPEndPoint[] tcpListenersArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            bool portIsBusy = tcpListenersArray.Any(tcp => tcp.Port == port);
            return portIsBusy;
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
