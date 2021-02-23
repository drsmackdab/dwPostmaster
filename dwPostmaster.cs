using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections.Specialized;

namespace dwPostmaster
{
    public class Postmaster
    {
        public enum eMethod
        {
            GET,
            HEAD,
            POST,
            PUT,
            DELETE,
            CONNECT,
            OPTIONS,
            TRACE,
            PATCH
        }
        private string sMethod;
        public NameValueCollection Fields { get; set; }
        public NameValueCollection Headers { get; set; }
        public string Body { get; set; }
        public Encoding Encoding { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public int requestTimeout { get; set; }
        public int ReadWriteTimeout { get; set; }
        public string URL { get; set; }
        public eMethod Method { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool useSSL { get; set; }
        public SecurityProtocolType securityProtocol { get; set; }
        public string Host { get; set; }
        public Version ProtocolVersion { get; set; }
        public Postmaster() {
            this.Fields = new NameValueCollection();
            this.Headers = new NameValueCollection();
            this.Method = eMethod.GET;
            this.securityProtocol = SecurityProtocolType.Tls12;
            this.ProtocolVersion = HttpVersion.Version11;
            this.useSSL = true;
            this.requestTimeout = System.Threading.Timeout.Infinite;
            this.ReadWriteTimeout = System.Threading.Timeout.Infinite;
            this.Encoding = Encoding.UTF8;
        }

        public string send(string QueryString = "")
        {
            string r = string.Empty;
            try {
                
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.URL);
                HttpWebResponse rsp = null;
                ServicePointManager.SecurityProtocol = this.securityProtocol;
                req.Host = this.Host;
                req.KeepAlive = true;
                req.Timeout = this.requestTimeout;
                req.ReadWriteTimeout = this.ReadWriteTimeout;
                req.ProtocolVersion = this.ProtocolVersion;
                req.Method = Enum.GetName(typeof(eMethod), this.Method);
                req.ContentType = this.ContentType;
                req.Accept = this.Accept;

                // add network credentials if any
                if (this.Username != string.Empty) {
                    req.Credentials = new NetworkCredential(this.Username, this.Password);
                }

                // add headers if any
                foreach (KeyValuePair<string,string> kvp in Headers)
                {
                    req.Headers.Add(kvp.Key, kvp.Value);
                }

                if (this.Method == eMethod.GET) { req.ContentLength = 0; }
                else if (this.Method == eMethod.POST || this.Method == eMethod.PUT)
                {

                    string postdata = string.Empty;

                    // if we have a body, use that.
                    if (this.Body.Length > 0) { postdata = this.Body; }
                    
                    // else if there are fields present, use those. 
                    else if (this.Fields.Count > 0) {
                        bool f = false;
                        foreach (KeyValuePair<string,string> kvp in this.Fields)
                        {
                            if (f == false) { postdata = string.Format("?{0}={1}",kvp.Key,kvp.Value); f = true; }
                            else { postdata += string.Format("&{0}={1}", kvp.Key, kvp.Value); }
                        }
                    }

                    // else use the supplied querystring
                    else { postdata = QueryString; }

                    // write body to request stream
                    byte[] buffer = this.Encoding.GetBytes(postdata);
                    req.ContentLength = buffer.Length;
                    Stream s = req.GetRequestStream();
                    s.Write(buffer, 0, buffer.Length);
                    s.Flush();
                    s.Close();
                    s.Dispose();
                }

                // try to get a response
                try { rsp = (HttpWebResponse)req.GetResponse(); }

                // server returned an error
                catch (WebException wex) { 
                    if (wex.Response != null)
                    {
                        using (Stream s = wex.Response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                r = sr.ReadToEnd();
                            }
                        }
                    }
                    else { r = wex.Message; }
                    return r;
                }

                // server returned a valid response
                using (StreamReader srv = new StreamReader(rsp.GetResponseStream(), this.Encoding))
                { r = srv.ReadToEnd(); }

                // clean up
                rsp.Close();
                rsp.Dispose();
                req = null;

            }

            catch (Exception ex) { r = ex.Message; }
            return r;
        }


    }
}
