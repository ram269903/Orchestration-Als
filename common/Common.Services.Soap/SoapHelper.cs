//http://www.c-sharpcorner.com/uploadfile/f9935e/invoking-a-web-service-dynamically-using-system-net-and-soap/
//https://forums.asp.net/t/1963372.aspx?Dynamic+call+of+SOAP+base+Web+Service+in+net
//http://www.c-sharpcorner.com/article/calling-web-service-using-soap-request/
//http://stackoverflow.com/questions/1261683/how-to-dynamically-invoke-web-services-in-net
//https://social.msdn.microsoft.com/Forums/windowsapps/en-US/15e0c692-c47c-443c-b96d-5c4f77e7ed66/how-to-sendreceive-soap-request-and-response-using-c-in-windows-phone-8?forum=wpdevelop
//http://www.diogonunes.com/blog/calling-webservice-without-wsdl-or-web-reference/
//http://stackoverflow.com/questions/4791794/client-to-send-soap-request-and-received-response
//http://stackoverflow.com/questions/14336414/httpclient-soap-c

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Common.Services.Soap
{
    public class SoapHelper
    {
        string _soapEnvelope =
        @"<soap:Envelope
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                    xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                <soap:Body></soap:Body></soap:Envelope>";

        private string _url;
        private string _webMethod;
        private string _contractName;
        private SoapServiceType _soapServiceType;
        private IList<Parameter> _parameters;

        public string CallSoapService(string url, string webMethod, string contractName, SoapServiceType soapServiceType, IList<Parameter> parameters, string xmlns = null)
        {
            _url = url;
            _webMethod = webMethod;
            _contractName = contractName;
            _soapServiceType = soapServiceType;
            _parameters = parameters;

            string responseString = string.Empty;
            HttpWebRequest request = CreateWebRequest();

            if (request != null)
            {
                var content = CreateSoapEnvelope(xmlns);
                if (!string.IsNullOrEmpty(content))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(content);
                    //using (var stream = await request.GetRequestStreamAsync())
                    //{
                    //    stream.Write(bytes, 0, content.Length);
                    //}
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, content.Length);
                    }
                }

                //using (var response = await request.GetResponseAsync())
                //{
                //    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                //}
                using (var response = request.GetResponse())
                {
                    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
            }

            //return StripResponse(WebUtility.HtmlDecode(responseString));

            return WebUtility.HtmlDecode(responseString);
        }

        private HttpWebRequest CreateWebRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_url);
            if (_soapServiceType == SoapServiceType.WCF)
                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + _contractName + "/" + _webMethod + "\"");
            else
                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + _webMethod + "\"");

            webRequest.Headers.Add("To", _url);

            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private string CreateSoapEnvelope(string xmlns)
        {
            string methodCall = string.Empty;
            string strParameters = string.Empty;

            if (string.IsNullOrEmpty(xmlns))
                methodCall = "<" + _webMethod + @" xmlns=""http://tempuri.org/"">";
            else
                methodCall = "<" + _webMethod + @" xmlns=" + xmlns + ">";

            foreach (var param in _parameters)
            {
                //strParameters = strParameters + "<" + param.Name + ">" + param.Value + "</" + param.Name + ">";
                strParameters += ParseParameter(param);
            }

            methodCall = methodCall + strParameters + "</" + _webMethod + ">";

            StringBuilder sb = new StringBuilder(_soapEnvelope);

            sb.Insert(sb.ToString().IndexOf("</soap:Body>"), methodCall);

            return sb.ToString();
        }

        private string ParseParameter(Parameter parameter)
        {
            var strParam = string.Empty;

            if (parameter.Value is string)
            {
                //Below line is commented  else not working in Digicert Registration
                if (_soapServiceType == SoapServiceType.TraditionalWCF)
                    strParam = "<" + parameter.Name + " xmlns=\"\">" + parameter.Value + "</" + parameter.Name + ">";
                else
                    strParam = "<" + parameter.Name + ">" + parameter.Value + "</" + parameter.Name + ">";
            }
            else if (parameter.Value is Dictionary<string, string>)
            {
                strParam = "<" + parameter.Name + " xmlns:a=\"http://schemas.datacontract.org/2004/07/OneCRSWcf\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" >";

                var dicParams = (Dictionary<string, string>)parameter.Value;

                foreach (var item in dicParams)
                {
                    if (item.Value != null)
                        strParam += "<a:" + item.Key + ">" + item.Value + "</a:" + item.Key + ">";
                    else
                        strParam += "<a:" + item.Key + " i:nil=\"true\"/>";
                }

                strParam += "</" + parameter.Name + ">";
            }

            return strParam;
        }

        //private string StripResponse(string soapResponse)
        //{
        //    string RegexExtract = @"<" + _webMethod + "Result>(?<Result>.*?)</" + _webMethod + "Result>";

        //    return Regex.Match(soapResponse, RegexExtract).Groups["Result"].Captures[0].Value;
        //}
    }

    public class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public enum SoapServiceType
    {
        TraditionalWCF = 0,
        WCF = 1
    }
}
