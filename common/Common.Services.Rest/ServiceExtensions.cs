using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PB.Common.Services
{
    public static class ServiceExtensions
    {
        private const string HttpHeaderApplicationJson = "application/json";
        private const string HttpHeaderApplicationXml = "application/xml";
        private const string HttpHeaderApplicationFormUrlEncoded = "application/x-www-form-urlencoded";

        public static async Task<T> GetAsync<T>(this Uri uri, Dictionary<string, string> headers = null, HttpContentType contentType = HttpContentType.Json)
        {
            T response;

            try
            {
                response = await HttpActionAsync<T>(HttpActionVerb.Get, uri, headers, null, contentType);
            }
            catch (Exception)
            {
                response = default(T);
            }

            return response;
        }

        public static async Task<T> GetAsync<T>(this Uri requestUrl)
        {
            return await GetAsync<T>(requestUrl);
        }

        public static async Task<T> PostAsync<T>(this Uri uri, string content, HttpContentType contentType = HttpContentType.Json)
        {
            T response;

            try
            {
                response = await HttpActionAsync<T>(HttpActionVerb.Post, uri, null, content, contentType);
            }
            catch (Exception)
            {
                response = default(T);
            }

            return response;
        }

        public static async Task<T> PostAsync<T>(this Uri uri, Dictionary<string, string> headers, string content, HttpContentType contentType = HttpContentType.Json)
        {
            T response;

            //try
            //{
            response = await HttpActionAsync<T>(HttpActionVerb.Post, uri, headers, content, contentType);
            //}
            //catch (Exception ex)
            //{
            //    response = default(T);
            //}

            return response;
        }

        public static async Task<T> PutAsync<T>(this Uri uri, string content)
        {
            return await HttpActionAsync<T>(HttpActionVerb.Put, uri, null, content);
        }

        public static async Task<T> DeleteAsync<T>(this Uri uri, Dictionary<string, string> headers)
        {
            return await HttpActionAsync<T>(HttpActionVerb.Delete, uri, headers, null);
        }

        private static async Task<T> HttpActionAsync<T>(HttpActionVerb verb, Uri uri, Dictionary<string, string> headers, string content, HttpContentType contentType = HttpContentType.Json)
        {
            string responseString = string.Empty;

            if (WebRequest.Create(uri) is HttpWebRequest request)
            {
                //request.Accept = acceptXml ? HttpHeaderApplicationXml : HttpHeaderApplicationJson;
                //request.ContentType = acceptXml ? HttpHeaderApplicationXml : HttpHeaderApplicationJson;

                switch (contentType)
                {
                    case HttpContentType.Json:
                        request.Accept = HttpHeaderApplicationJson;
                        request.ContentType = HttpHeaderApplicationJson;
                        break;
                    case HttpContentType.Xml:
                        request.Accept = HttpHeaderApplicationXml;
                        request.ContentType = HttpHeaderApplicationXml;
                        break;
                    case HttpContentType.FormUrlEncoded:
                        request.Accept = "text/plain";
                        request.ContentType = HttpHeaderApplicationFormUrlEncoded;
                        break;
                    default:
                        request.Accept = HttpHeaderApplicationJson;
                        request.ContentType = HttpHeaderApplicationJson;
                        break;
                }

                //request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                request.Method = VerbString(verb);
                //request.Timeout = 300000;

                if (headers != null && headers.Count > 0)
                {

                    foreach (var key in headers.Keys)
                    {
                        request.Headers[key] = headers[key];
                    }
                }
                if (!string.IsNullOrEmpty(content))
                {
                    byte[] bytes = System.Text.Encoding.ASCII.GetBytes(content);
                    using (var stream = await request.GetRequestStreamAsync())
                    {
                        stream.Write(bytes, 0, content.Length);
                    }
                }

                //try
                //{
                //    var response = await request.GetResponseAsync();
                //    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                //}
                //catch (Exception e)
                //{
                //    var s = e.InnerException.Message;

                //}

                try
                {
                    using (var response = await request.GetResponseAsync())
                    {
                        if (request.HaveResponse && response != null)
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                responseString = reader.ReadToEnd();
                            }
                        }
                    }

                    if (typeof(T).Name == "String")
                        return (T)(object)responseString;
                    else
                        return JsonConvert.DeserializeObject<T>(responseString);
                }
                catch (WebException wex)
                {
                    if (wex.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)wex.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                string error = reader.ReadToEnd();
                                throw new Exception(error);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Error processing request.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error processing request. " + ex.Message);
                }
            }
            else
                return default(T);
        }

        static string VerbString(HttpActionVerb verb)
        {
            switch (verb)
            {
                case HttpActionVerb.Delete:
                    return "DELETE";
                case HttpActionVerb.Get:
                    return "GET";
                case HttpActionVerb.Post:
                    return "POST";
                case HttpActionVerb.Put:
                    return "PUT";
                default:
                    return "GET";
            }
        }

        enum HttpActionVerb
        {
            Get,
            Post,
            Delete,
            Put,
        }

        public enum HttpContentType
        {
            Json,
            Xml,
            FormUrlEncoded
        }
    }
}
