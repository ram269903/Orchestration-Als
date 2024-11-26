//using Common.Communication.Model;
//using System;
//using System.IO;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace Common.Communication
//{
//    public class SmsHelper
//    {
//        private readonly SmsRepository _smsRepository;
//        private readonly SmsSettings _smsSettings;

//        public SmsHelper(SmsConfig smsConfig)
//        {
//            _smsConfig = new SmsRepository(_connectionString);
//            _smsSettings = smsSettings;
//        }

//        public async Task<bool> CheckSmsSent(string enterpriseId, int month, int year, StatementType statementType)
//        {
//            return await _smsRepository.CheckSmsSent(enterpriseId, month, year, statementType);
//        }


//        public async Task<string> SendSmsMessage(string enterpriseId, string mobileNumber, string shortUrlCode, int month, int year, StatementType statementType, string alertType)
//        {
//            var smsMessage = new StringBuilder(_smsSettings.SmsTemplate);

//            smsMessage.Replace("{MobileNumber}", mobileNumber)
//                .Replace("{StatementCode}", shortUrlCode)
//                .Replace("{MessageId}", WebUtility.UrlEncode(enterpriseId.ToString()) ?? throw new InvalidOperationException("enterpriseId"));

//            var response =  await HttpGetAsync<string>(new Uri(_smsSettings.Url), smsMessage.ToString());

//            if (!string.IsNullOrEmpty(response))
//            {
//                await _smsRepository.InsertSmsSent(new SmsSent
//                {
//                    EnterpriseId = enterpriseId,
//                    MobileNumber = mobileNumber,
//                    StatementType = statementType,
//                    Message = smsMessage.ToString(),
//                    Month = month,
//                    Year = year,
//                    AlertType = alertType,
//                    Response = response
//                });
//            }

//            return response;

//        }


//        private async Task<T> HttpGetAsync<T>(Uri uri, string content)
//        {
//            string responseString = string.Empty;

//            if (WebRequest.Create(uri) is HttpWebRequest request)
//            {
//                request.Accept = "text/plain";
//                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

//                //request.Accept = "application/json";
//                //request.ContentType = "application/json";

//                //request.Proxy.Credentials = CredentialCache.DefaultCredentials;
//                request.Credentials = CredentialCache.DefaultNetworkCredentials;
//                request.Method = "GET";
//                //request.Timeout = 300000;

//                if (!string.IsNullOrEmpty(content))
//                {
//                    byte[] bytes = Encoding.ASCII.GetBytes(content);
//                    using (var stream = await request.GetRequestStreamAsync())
//                    {
//                        stream.Write(bytes, 0, content.Length);
//                    }
//                }

//                try
//                {
//                    using (var response = await request.GetResponseAsync())
//                    {
//                        if (request.HaveResponse && response != null)
//                        {
//                            using (var reader = new StreamReader(response.GetResponseStream()))
//                            {
//                                responseString = reader.ReadToEnd();
//                            }
//                        }
//                    }

//                    if (typeof(T).Name == "String")
//                        return (T)(object)responseString;
//                    else
//                        return JsonConvert.DeserializeObject<T>(responseString);
//                }
//                catch (WebException wex)
//                {
//                    if (wex.Response != null)
//                    {
//                        using (var errorResponse = (HttpWebResponse)wex.Response)
//                        {
//                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
//                            {
//                                string error = reader.ReadToEnd();
//                                throw new Exception(error);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        throw new Exception("Error processing request.");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    throw new Exception("Error processing request. " + ex.Message);
//                }
//            }
//            else
//                return default(T);
//        }
//    }
//}
