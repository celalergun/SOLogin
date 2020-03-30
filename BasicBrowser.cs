using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace SOLogin
{
    /// <summary>
    /// http://refactoringaspnet.blogspot.com/2010/04/using-htmlagilitypack-to-get-and-post.html
    /// 
    /// Örnek kullanım: 
    /// BrowserSession b = new BrowserSession();
    /// b.Get("http://my.site.com/login.aspx");
    /// b.FormElements["loginTextBox"] = "username";
    /// b.FormElements["passwordTextBox"] = "password";
    /// string response = b.Post("http://my.site.com/login.aspx");
    /// </summary>
    public class BasicBrowser
    {
        private bool _isPost;
        private bool _isDownload;
        private string _download;
        public HtmlDocument HtmlDoc;

        /// <summary>
        /// System.Net.CookieCollection. Provides a collection container for instances of Cookie class 
        /// </summary>
        public CookieCollection Cookies { get; set; }

        /// <summary>
        /// Provide a key-value-pair collection of form elements 
        /// </summary>
        public FormElementCollection FormElements { get; set; }

        /// <summary>
        /// Makes a HTTP GET request to the given URL
        /// </summary>
        public string Get(string url)
        {
            _isPost = false;
            CreateWebRequest().Load(url);
            return HtmlDoc.DocumentNode.InnerHtml;
        }

        /// <summary>
        /// Makes a HTTP POST request to the given URL
        /// </summary>
        public string Post(string url)
        {
            _isPost = true;
            CreateWebRequest().Load(url, "POST");
            if (String.IsNullOrEmpty(HtmlDoc.DocumentNode.InnerText))
                return "";
            else
                return HtmlDoc.DocumentNode.InnerHtml;
        }

        public string GetDownload(string url)
        {
            _isPost = false;
            _isDownload = true;
            CreateWebRequest().Load(url);
            return _download;
        }

        /// <summary>
        /// Creates the HtmlWeb object and initializes all event handlers. 
        /// </summary>
        private HtmlWeb CreateWebRequest()
        {
            HtmlWeb web = new HtmlWeb
            {
                UseCookies = true,
                PreRequest = new HtmlWeb.PreRequestHandler(OnPreRequest),
                PostResponse = new HtmlWeb.PostResponseHandler(OnAfterResponse),
                PreHandleDocument = new HtmlWeb.PreHandleDocumentHandler(OnPreHandleDocument),
                UserAgent = RandomUserAgentString()
            };
            return web;
        }


        /// <summary>
        /// Event handler for HtmlWeb.PreRequestHandler. Occurs before an HTTP request is executed.
        /// </summary>
        protected bool OnPreRequest(HttpWebRequest request)
        {
            AddCookiesTo(request);               // Add cookies that were saved from previous requests
            if (_isPost) AddPostDataTo(request); // We only need to add post data on a POST request
            return true;
        }

        /// <summary>
        /// Event handler for HtmlWeb.PostResponseHandler. Occurs after a HTTP response is received
        /// </summary>
        protected void OnAfterResponse(HttpWebRequest request, HttpWebResponse response)
        {
            SaveCookiesFrom(request, response); // Save cookies for subsequent requests

            if (response != null && _isDownload)
            {
                Stream remoteStream = response.GetResponseStream();
                var sr = new StreamReader(remoteStream);
                _download = sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Event handler for HtmlWeb.PreHandleDocumentHandler. Occurs before a HTML document is handled
        /// </summary>
        protected void OnPreHandleDocument(HtmlDocument document)
        {
            SaveHtmlDocument(document);
        }

        /// <summary>
        /// Assembles the Post data and attaches to the request object
        /// </summary>
        private void AddPostDataTo(HttpWebRequest request)
        {
            string payload = FormElements.AssemblePostPayload();
            byte[] buff = Encoding.UTF8.GetBytes(payload.ToCharArray());
            request.ContentLength = buff.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            System.IO.Stream reqStream = request.GetRequestStream();
            reqStream.Write(buff, 0, buff.Length);
        }

        /// <summary>
        /// Add cookies to the request object
        /// </summary>
        private void AddCookiesTo(HttpWebRequest request)
        {
            if (Cookies != null && Cookies.Count > 0)
            {
                request.CookieContainer.Add(Cookies);
            }
        }

        /// <summary>
        /// Saves cookies from the response object to the local CookieCollection object
        /// </summary>
        private void SaveCookiesFrom(HttpWebRequest request, HttpWebResponse response)
        {
            if (request.CookieContainer.Count > 0 || response.Cookies.Count > 0)
            {
                if (Cookies == null)
                {
                    Cookies = new CookieCollection();
                }

                Cookies.Add(request.CookieContainer.GetCookies(request.RequestUri));
                Cookies.Add(response.Cookies);
            }
        }

        /// <summary>
        /// Saves the form elements collection by parsing the HTML document
        /// </summary>
        private void SaveHtmlDocument(HtmlDocument document)
        {
            HtmlDoc = document;
            FormElements = new FormElementCollection(HtmlDoc);
        }

        public static void CerezleriGuncelle(ref string cookie, string modelCookie)
        {
            if (cookie == null)
            {
                cookie = modelCookie;
                return;
            }
            var newCookies = modelCookie.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var oldCookies = cookie.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            for (int i = 0; i < oldCookies.Length; i++)
            {
                string s = oldCookies[i];
                int pos = s.IndexOf('=');
                string tmp1 = s.Substring(0, pos);
                string tmp2 = s.Substring(pos + 1, s.Length - pos - 1);
                dictionary.Add(tmp1, tmp2);
            }

            for (int i = 0; i < newCookies.Length; i++)
            {
                var kv = newCookies[i].Split('=');
                if (dictionary.ContainsKey(kv[0]))
                {
                    dictionary[kv[0]] = kv[1];
                }
                else
                {
                    dictionary.Add(kv[0], kv[1]);
                }
            }

            string result = "";
            foreach (var k in dictionary)
            {
                result += k.Key + '=' + k.Value + ';';
            }

            cookie = result;
        }


        #region UserAgent strings
        private static readonly string[] UserAgentSignatures = {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.103 Safari/537.36 OPR/60.0.3255.84",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:67.0) Gecko/20100101 Firefox/67.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36 OPR/62.0.3331.72",
            "Mozilla/5.0 (Windows NT 6.1; rv:60.0) Gecko/20100101 Firefox/60.0",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.80 Safari/537.36 OPR/62.0.3331.18",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:68.0) Gecko/20100101 Firefox/68.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.120 Safari/537.36 OPR/64.0.3417.92",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36 OPR/38.0.2220.41",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 10_3_1 like Mac OS X) AppleWebKit/603.1.30 (KHTML, like Gecko) Version/10.0 Mobile/14E304 Safari/602.1",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0)",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:70.0) Gecko/20100101 Firefox/70.0",
        };

        private static string RandomUserAgentString()
        {
            Random r = new Random();
            return UserAgentSignatures[r.Next(Int32.MaxValue) % UserAgentSignatures.Length];
        }
        #endregion
    }
}
