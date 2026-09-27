using SWTSdk.NetSocket;
using System;
using System.Net;
using System.Text;
using System.IO;
using System.Net.Security;

namespace MiniAtmArtaJasa.Library
{
    internal class XHttpService
    {
        public XHttpService()
        {
            //issue with https
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Certificate.ValidateRemoteCertificate);

            //issue default limit is 2
            ServicePointManager.DefaultConnectionLimit = 100;

            //solution for error: Could not create SSL/TLS secure channel
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public WSResponse Post(string URL, string JSON, int Timeout = 30)
        {
            WSResponse retval = new WSResponse();

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(URL);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = JSON.Length;
                webRequest.Method = "POST";
                webRequest.Timeout = Timeout * 1000;

                //issuer first connection delay
                webRequest.Proxy = null;

                byte[] bytes = Encoding.UTF8.GetBytes(JSON);

                Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Flush();

                requestStream.Close();

                HttpWebResponse wsResponse = (HttpWebResponse)webRequest.GetResponse();

                string responseString = string.Empty;
                if (wsResponse.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = wsResponse.GetResponseStream();
                    responseString = new StreamReader(responseStream).ReadToEnd();
                }

                //set data response
                retval.StatusCode = wsResponse.StatusCode;
                retval.MsgResponse = responseString;

                // Release the HttpWebResponse
                wsResponse.Close();
            }
            catch (Exception ex)
            {
                retval.StatusCode = HttpStatusCode.BadRequest;
                retval.MsgError = ex.Message;
            }

            return retval;
        }
    }
}
