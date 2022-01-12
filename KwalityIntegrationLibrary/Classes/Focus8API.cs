using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace KwalityIntegrationLibrary.Models
{
    public class Focus8API
    {
        public static string Post(string url, string data, string sessionId, ref string err)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("fSessionId", sessionId);
                    client.Headers.Add("Content-Type", "application/json");
                    var response = client.UploadString(url, data);

                    return response;
                }
            }
            catch (Exception e)
            {

                err = e.Message;
                return null;
            }

        }

        public static string Get(string url, string sessionId, ref string err)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("fSessionId", sessionId);
                    //client.Headers.Add("Content-Type", "application/json");
                    var response = client.DownloadString(url);
                    return response;
                }
            }
            catch (Exception e)
            {
                err = e.Message;
                return null;
            }

        }
        public static string Delete(string url, string sessionId, ref string err)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("fSessionId", sessionId);
                    client.Headers.Add("Content-Type", "application/json");
                    var response = client.UploadString(url,"DELETE", "");

                    return response;
                }
            }
            catch (Exception e)
            {

                err = e.Message;
                return null;
            }

        }
    }
}