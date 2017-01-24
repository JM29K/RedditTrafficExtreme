using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedditTrafficExtreme
{
    public class RegularWebRequest : WebClient
    {
        private int timeout;
        public RegularWebRequest(int timeoutSeconds)
            {
               
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

           


            protected override WebRequest GetWebRequest(Uri address)
            {
              
               WebRequest r = base.GetWebRequest(address);
                var request = r as HttpWebRequest;
                if (request != null)
                {
                  request.Headers.Add("Accept-Language: en-US");
                  

                    System.Net.ServicePointManager.Expect100Continue = false;
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
                
                    request.Timeout = timeout * 1000;
                    request.ReadWriteTimeout = timeout * 1000;
                    
                }
                return r;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
            
                
                
                WebResponse response = base.GetWebResponse(request, result);
             
                return response;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
              
                WebResponse response = base.GetWebResponse(request);
               
                return response;
            }

           
        
    }
}
