using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedditTrafficExtreme
{
    public class RegularProxyWebRequest : WebClient
    {
        private int timeout;
        public RegularProxyWebRequest(int timeoutSeconds, CookieContainer container, List<WebProxy> proxylist)
            {
                this.container = container;
                this.proxys = proxylist;
                this.Encoding = Encoding.ASCII;
                if (timeout == 0)
                {
                    timeout = 5;
                }
                else
                {
                    timeout = timeoutSeconds;
                }
            }

            private readonly CookieContainer container = new CookieContainer();
            private readonly List<WebProxy> proxys = new List<WebProxy>();
            

            public int proxyUsed
            {
                get;
                set;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                
                
                DataEntities modelcontainer = new DataEntities();
                bool goodProxy = true;
                int i = 0;
                Random rand = new Random();
                do
                {
                    if (i == (proxys.Count))
                    {
                        return null;
                    }
                int chosenproxy = i;
                string url = string.Format("{0}", proxys[chosenproxy].Address.Host);
                ProxiesLoaded p = modelcontainer.ProxiesLoadeds.ToList().Single(s => s.URL == url);
                if ((p.NextUse == null || p.NextUse <= DateTime.Now) && (!p.InUse || i == 0))
                {
                    this.Proxy = proxys[chosenproxy];
                    goodProxy = true;
                    proxyUsed = p.ID;
                }
                else
                {
                    goodProxy = false;
                }
                i++;
                }
                while (goodProxy == false);
                WebRequest r = base.GetWebRequest(address);
                var request = r as HttpWebRequest;
                if (request != null)
                {
                    request.CookieContainer = container;
                 //   request.Headers.Add("Accept-Encoding:gzip, deflate");
                    request.Headers.Add("Accept-Language: en-US");
                    //request.Headers.Add("Pragma: no-cache");
                    request.CookieContainer.Add(new Cookie("__utmc","55650728","/","reddit.com"));
                    request.CookieContainer.Add(new Cookie("__utma","55650728.338413115.1362520679.1362520679.1362520679.1","/","reddit.com"));
                    request.CookieContainer.Add(new Cookie("__utmb","55650728.1.10.1362520679","/","reddit.com"));
                    request.CookieContainer.Add(new Cookie("__utmz","55650728.1362520679.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)","/","reddit.com"));


                    System.Net.ServicePointManager.Expect100Continue = false;
                    ServicePointManager.DefaultConnectionLimit = 200;
                    if (request.Method == "GET")
                    {
                        if (request.RequestUri.AbsolutePath.Contains("/captcha"))
                        {
                            request.Accept = "image/png, image/svg+xml, image/*;q=0.8, */*;q=0.5";
                        }
                        else
                        {
                            request.Accept = "text/html, application/xhtml+xml, */*";
                        }
                    }
                    else
                    {
                    //    request.Connection = "keep-alive";
                             request.Accept = "application/json, text/javascript, */*; q=0.01";
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    }
                    request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";



                    
                    request.Proxy = Proxy;
                    request.Timeout = timeout * 1000;
                    request.ReadWriteTimeout = timeout * 1000;
                    
                    
                }
                return r;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                
                
                
                WebResponse response = base.GetWebResponse(request, result);
                ReadCookies(response);
                return response;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                
                
                WebResponse response = base.GetWebResponse(request);
                ReadCookies(response);
                return response;
            }

            private void ReadCookies(WebResponse r)
            {
                
                
                
                var response = r as HttpWebResponse;
                if (response != null)
                {
                    CookieCollection cookies = response.Cookies;
                    container.Add(cookies);
                }
            }
        
    }
}
