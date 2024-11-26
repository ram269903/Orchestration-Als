using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static PB.Common.Services.ServiceExtensions;

namespace PB.Common.Services
{
    public class RestServiceHelper
    {
        public async Task<T> GetServiceAsync<T>(string url, Dictionary<string, string> customHeaders = null, HttpContentType contentType = HttpContentType.Json)
        {
            return await new Uri(url).GetAsync<T>(customHeaders, contentType);
        }

        public async Task<T> PostServiceAsync<T>(string url, object data, Dictionary<string, string> customHeaders = null, HttpContentType contentType = HttpContentType.Json)
        {
            var content = string.Empty;

            if (data is string)
                content = (string)data;
            else
                content = JsonConvert.SerializeObject(data);

            var res = new Uri(url).PostAsync<T>(customHeaders, content, contentType);

            return await res;
        }

        public async Task<T> DeleteServiceAsync<T>(string url, Dictionary<string, string> customHeaders = null, HttpContentType contentType = HttpContentType.Json)
        {
            return await new Uri(url).DeleteAsync<T>(customHeaders);
        }

        //public async Task<string> GetServiceAsync(string url, Dictionary<string, string> customHeaders = null)
        //{
        //    var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
        //    request.ContentType = "application/json";
        //    request.Method = "GET";

        //    if (customHeaders != null && customHeaders.Count > 0)
        //    {
        //        foreach (var key in customHeaders.Keys)
        //        {
        //            request.Headers[key] = customHeaders[key];
        //        }
        //    }

        //    using (var response = await request.GetResponseAsync())
        //    {
        //        // Get a stream representation of the HTTP web response:
        //        using (var stream = response.GetResponseStream())
        //        {
        //            //return await Task.Run(() => JsonObject.Load(stream));

        //            var jsonDoc = await Task.Run(() => new StreamReader(stream));
        //            return jsonDoc.ReadToEnd();
        //        }
        //    }
        //}

        //public async Task<string> PostServiceAsync(string url, object data, Dictionary<string, string> customHeaders = null)
        //{
        //    string responseString;
        //    var jsonDataString = JsonConvert.SerializeObject(data);

        //    var postData = Encoding.ASCII.GetBytes(jsonDataString);

        //    var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
        //    request.ContentType = "application/json";
        //    request.Method = "POST";
        //    request.ContinueTimeout = 2000;
        //    //request.ContentLength = jsonDataString.Length;

        //    if (customHeaders != null && customHeaders.Count > 0)
        //    {
        //        foreach (var key in customHeaders.Keys)
        //        {
        //            request.Headers[key] = customHeaders[key];
        //        }
        //    }

        //    using (var stream = await request.GetRequestStreamAsync())
        //    {
        //        stream.Write(postData, 0, jsonDataString.Length);

        //        using (var response = await request.GetResponseAsync())
        //        {
        //            responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        //        }

        //    }

        //    return responseString;
        //}
    }
}
