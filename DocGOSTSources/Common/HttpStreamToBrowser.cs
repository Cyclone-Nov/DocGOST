﻿using System;
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

        /// <summary>
        /// адрес URI для приема байтового массива с pdf страницей в виде сообщения 
        /// </summary>
        /// <value>
        /// The host URI.
        /// </value>
        public string HostUri { get; private set; }

        HttpDataToBrowser()
        {
            for (int port = 40000; port < 45000; port++)
            {
                if (!IsPortBusy(port))
                {
                    HostUri = $"http://localhost:{port}/pdf/";
                    _httpListener.Prefixes.Add(HostUri);
                    _httpListener.Start();
                    break;
                }
            }
        }

        private bool IsPortBusy(int port)
        {
            System.Net.IPEndPoint[] tcpListenersArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            bool portIsBusy = tcpListenersArray.Any(tcp => tcp.Port == port);
            return portIsBusy;
        }

        /// <summary>
        /// установить pdf страницу в виде массива байт для отображения в браузере
        /// </summary>
        /// <param name="aData">массив байт, содержащий pdf страницу</param>
        /// <returns>результат асинхронной операции типа <paramref name="IAsyncResult"/></returns>
        public IAsyncResult SetData(byte[] aData)
        {
            return _httpListener.BeginGetContext((ar) => {

                var listener = (HttpListener)ar.AsyncState;
                HttpListenerContext context = listener.EndGetContext(ar);

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
            }, _httpListener);
        }

    }
}
