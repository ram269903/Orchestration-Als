using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DSS.Orchestration.CommonProcess
{
    public static class MailProcess
    {
        private const string HttpHeaderApplicationJson = "application/json";
        private const string HttpHeaderApplicationXml = "application/xml";
        private const string HttpHeaderApplicationFormUrlEncoded = "application/x-www-form-urlencoded";

        public static async Task<string> UrlHit(string url)
        {

            HttpContentType contentType = HttpContentType.Json;

            var verb = "GET";

            Dictionary<string, string> headers = null;

            string content = null;

            string responseString = string.Empty;

            if (WebRequest.Create(url) is HttpWebRequest request)
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
                request.Method = verb;
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

                    if (typeof(string).Name == "String")
                        return (string)(object)responseString;
                    else
                        return JsonConvert.DeserializeObject<string>(responseString);
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
                return default(string);
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

        //public static bool MailSend()
        //{
        //    Log.Information("Mail send process initiated..");
        //    try
        //    {
        //        var web = new HttpClient();

        //        var body = File.ReadAllText(_mailSettings.BodyFilePath);

        //        body = body.Replace("{datetime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        //        string bodyPath = _mailSettings.BodyFilePath.Replace(Path.GetFileName(_mailSettings.BodyFilePath), "") + "Body.txt";

        //        File.WriteAllText(bodyPath, body);

        //        File.WriteAllText(_mailSettings.SubjectFilePath, "PROCESS FAILURE");

        //        var url = $"{_mailSettings.ApiUrl}/api/mail?sp={_mailSettings.SubjectFilePath}&bp={bodyPath}";

        //        Log.Information("Mail url :" + url);

        //        var responseString = web.GetStringAsync(url).Result;

        //        Log.Information("Got result from mail url is:" + responseString);

        //        Log.Information("Mail send process completed..");

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "Error while sending Mail");
        //        return false;
        //    }
        //}
    }
}
