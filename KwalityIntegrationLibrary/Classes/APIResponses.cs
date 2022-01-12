using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KwalityIntegrationLibrary.Models
{
    public class APIResponse
    {
        public class Data
        {
            public List<Hashtable> Body { get; set; }
            public Hashtable Header { get; set; }
            public List<Hashtable> Footer { get; set; }
        }

        public class Response
        {
            public string url { get; set; }
            public List<Data> data { get; set; }
            public int result { get; set; }
            public string message { get; set; }
        }
        public class PostResponse
        {
            public string url { get; set; }
            public List<Hashtable> data { get; set; }
            public int result { get; set; }
            public string message { get; set; }
        }
        public partial class Temperatures
        {
            [JsonProperty("data")]
            public Datum[] Data { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("result")]
            public long Result { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
        public partial class Datum
        {
            [JsonProperty("fSessionId")]
            public string FSessionId { get; set; }
        }
    }
}