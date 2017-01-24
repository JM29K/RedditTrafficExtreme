using DeathByCaptcha;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RedditTrafficExtreme
{
    public class LinkSubmitter
    {
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        CancellationTokenSource source = new CancellationTokenSource();
        private System.Timers.Timer timer = new System.Timers.Timer();
        private System.Timers.Timer timer2 = new System.Timers.Timer();
        static readonly object _object = new object();
        static readonly object _recordNumberObject = new object();
        List<Task<bool>> asyncCalls = new List<Task<bool>>();
        RedditForm Form = null;

        public struct LinkInfo
        {
            public LinkInfo(bool sub, bool IsItInUse)
            {
                submitted = sub;
                inUse = IsItInUse;
            }
            public bool submitted;
            public bool inUse;
        }

        private int numLinksSubmitted = 0;
        public int NumLinksSubmitted
        {
            get { lock (_object) { return numLinksSubmitted; } }
            set { lock (_object) { numLinksSubmitted = value; } }
        }

        private Dictionary<int, LinkInfo> records = new Dictionary<int, LinkInfo>();
        public void MarkRecordComplete(int recordNumber)
        {
            lock (_recordNumberObject)
            {
                LinkInfo l = records[recordNumber];
                l.inUse = false;
                l.submitted = true;
                records[recordNumber] = l;
            }
        }

        public void MarkRecordInUseOrNot(int recordNumber,bool inUse)
        {
            lock (_recordNumberObject)
            {
                LinkInfo l = records[recordNumber];
                l.inUse = inUse;
                
                records[recordNumber] = l;
            }
        }

        
        

        private int numThreadsRunning = 0;
        public int NumThreadsRunning
        {
            get { lock (_object) { return numThreadsRunning; } }
            set { lock (_object) { numThreadsRunning = value; } }
        }

        private bool threadsEnded = false;
        public bool ThreadsEnded
        {
            get { lock (_object) { return threadsEnded; } }
            set { lock (_object) { threadsEnded = value; } }
        }

        public LinkSubmitter(RedditForm form)
        {
            Form = form;
            timer.Enabled = false;
            timer.Interval = 60000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer1_Tick);
            timer2.Enabled = false;
            timer2.Interval = 6000;
            timer2.Elapsed += new System.Timers.ElapsedEventHandler(timer2_Tick);
        }

        private void WriteToLog(string logText)
        {
            if (Form.LinkSubmitProcessLog.InvokeRequired)
            {
                Form.LinkSubmitProcessLog.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitProcessLog.Text = logText + Environment.NewLine + Form.LinkSubmitProcessLog.Text; ;});
            }
            else
            {
                Form.LinkSubmitProcessLog.Text = logText + Environment.NewLine + Form.LinkSubmitProcessLog.Text;
            }
        }

        private void UpdateProgress(int i)
        {

            if (Form.LinkSubmitProgress.InvokeRequired)
            {
                Form.LinkSubmitProgress.BeginInvoke((MethodInvoker)delegate() { if (Form.LinkSubmitProgress.Value1 != Convert.ToInt32(Form.LinkSubmitNumToCreate.Text)) { Form.LinkSubmitProgress.Value1 = Form.LinkSubmitProgress.Value1 + i; } });
            }
            else
            {
                if (Form.LinkSubmitProgress.Value1 != Convert.ToInt32(Form.LinkSubmitNumToCreate.Text))
                {
                    Form.LinkSubmitProgress.Value1 = Form.LinkSubmitProgress.Value1 = Form.LinkSubmitProgress.Value1 + i;
                }
            }

            if (Form.LinksSubmittedlbl.InvokeRequired)
            {
                Form.LinksSubmittedlbl.BeginInvoke((MethodInvoker)delegate() { Form.LinksSubmittedlbl.Text = Convert.ToString(Convert.ToInt32(Form.LinksSubmittedlbl.Text) + i); });
            }
            else
            {
                Form.LinksSubmittedlbl.Text = Convert.ToString(Convert.ToInt32(Form.LinksSubmittedlbl.Text) + i);
            }

        }

        private void DecreaseThreadCountLbl(int i)
        {
            if (NumThreadsRunning != 0)
            {
                if (Form.LinkSubmitThreadsRunning.InvokeRequired)
                {
                    Form.LinkSubmitThreadsRunning.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.LinkSubmitThreadsRunning.Text) - 1); ;});
                }
                else
                {
                    Form.LinkSubmitThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.LinkSubmitThreadsRunning.Text) - 1);
                }
                NumThreadsRunning = NumThreadsRunning - 1;
            }
            if (NumThreadsRunning <= 0)
            {
                Form.LinkSubmitProgress.BeginInvoke((MethodInvoker)delegate()
                {
                    if (Form.LinkSubmitProgress.Value1 != Form.LinkSubmitProgress.Maximum)
                    {
                        if (ThreadsEnded == false)
                        {
                            WriteToLog("No Threads running but not all links submitted!");
                            ThreadsEnded = true;
                        }
                    }
                });
                Form.LinkSubmitNumThreadsCreate.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitNumThreadsCreate.Text = "0"; Form.LinkSubmitNumThreadsCreate.Enabled = true; });
                Form.SubmitLinks.BeginInvoke((MethodInvoker)delegate() { Form.SubmitLinks.Enabled = true; ;});

                _resetEvent.Set();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _pauseEvent.Set();
            timer.Enabled = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            int maxThreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out maxThreads, out completionPortThreads);

            int availableThreads;
            ThreadPool.GetAvailableThreads(out availableThreads, out completionPortThreads);

            if (maxThreads - availableThreads == 1)
            {

                NumThreadsRunning = 0;

                Form.LinkSubmitNumThreadsCreate.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitNumThreadsCreate.Text = "0"; Form.LinkSubmitNumThreadsCreate.Enabled = true; });
                Form.SubmitLinks.BeginInvoke((MethodInvoker)delegate() { Form.SubmitLinks.Enabled = true; ;});
                Form.LinkSubmitThreadsRunning.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitThreadsRunning.Text = "0"; });
                _resetEvent.Set();
            }
        }

        #region LinkSubmitter
        public void BindSubRedditList()
        {
            Form.SubRedditWaiting.Visible = true;
            Form.SubRedditWaiting.StartWaiting();
            string selectedFilter = "media";
            Form.LinkSubmitSubReddits.Items.Clear();
            if (Form.LinkSubmitterCommonFilter.SelectedIndex != -1)
            {
                selectedFilter = Form.LinkSubmitterCommonFilter.SelectedText.ToLower();
            }
            Task.Factory.StartNew(() =>  GetSubreddits(selectedFilter)).ContinueWith(t =>
            {
                Form.SubRedditWaiting.BeginInvoke((MethodInvoker)delegate()
                {
                    Form.SubRedditWaiting.StartWaiting();
                    Form.SubRedditWaiting.Visible = false;
                });
            });
           
        }

        



        public void LinkSubmitSubredditFilter_TextChanged()
        {
            if (Form.LinkSubmitSubredditFilter.Text.Length < 3 && Form.LinkSubmitSubredditFiltered.Items.Count == 0)
            {
                return;
            }
            else if (Form.LinkSubmitSubredditFilter.Text.Length < 3 && Form.LinkSubmitSubredditFiltered.Items.Count != 0)
            {
                List<string> inListAndNot = new List<string>();
                inListAndNot.AddRange(Form.LinkSubmitSubredditFiltered.Items.OfType<string>());
                inListAndNot.AddRange(Form.LinkSubmitSubReddits.Items.OfType<string>());
                Form.LinkSubmitSubredditFiltered.Items.Clear();
                Form.LinkSubmitSubReddits.Items.Clear();
                Form.LinkSubmitSubReddits.Items.AddRange(inListAndNot.ToArray());

            }
            else
            {


                List<string> inListAndNot = new List<string>();
                inListAndNot.AddRange(Form.LinkSubmitSubredditFiltered.Items.OfType<string>());
                inListAndNot.AddRange(Form.LinkSubmitSubReddits.Items.OfType<string>());
                inListAndNot = inListAndNot.Distinct().ToList();
                Form.LinkSubmitSubredditFiltered.Items.Clear();
                Form.LinkSubmitSubReddits.Items.Clear();

                foreach (string i2 in inListAndNot)
                {
                    if (i2.ToLower().Contains(Form.LinkSubmitSubredditFilter.Text.ToLower()))
                    {
                        Form.LinkSubmitSubReddits.Items.Add(i2);
                    }
                    else
                    {
                        Form.LinkSubmitSubredditFiltered.Items.Add(i2);
                    }
                }
            }

        }

        private void GetSubreddits(string selectedFilter)
        {
            
            DataEntities container2 = new DataEntities();
            List<string> subredditNames = new List<string>();
            RegularWebRequest cw = CreateRegularWebRequest();
            var response = Encoding.ASCII.GetString(cw.DownloadData(string.Format("http://metareddit.com/reddits/{0}/list", selectedFilter)));
            HtmlAgilityPack.HtmlDocument pageHtml = new HtmlAgilityPack.HtmlDocument();
            pageHtml.LoadHtml(response);
            int totalReddits = 0; int totalPages = 0;
            totalReddits = Convert.ToInt32(pageHtml.DocumentNode.SelectSingleNode(@"//div[@class=""title""]/strong").InnerText.Replace("\n", "").Replace(" ", ""));
            totalPages = totalReddits / 50;
            HtmlNodeCollection subrededits = pageHtml.DocumentNode.SelectNodes(@"//a[@class=""subreddit""]");
            foreach (HtmlNode h in subrededits)
            {
                subredditNames.Add(h.InnerText);
            }
            Form.LinkSubmitSubReddits.BeginInvoke((MethodInvoker)delegate()
                {
                    Form.LinkSubmitSubReddits.Items.AddRange(subredditNames.ToArray());
                });

            for (int i = 2; i < totalPages; i++)
            {
                response = Encoding.ASCII.GetString(cw.DownloadData(string.Format("http://metareddit.com/reddits/{1}/list?page={0}", i, selectedFilter)));
                pageHtml.LoadHtml(response);

                subrededits = pageHtml.DocumentNode.SelectNodes(@"//a[@class=""subreddit""]");
                foreach (HtmlNode h in subrededits)
                {
                    subredditNames.Add(h.InnerText);
                }
                Form.LinkSubmitSubReddits.BeginInvoke((MethodInvoker)delegate()
                {
                    Form.LinkSubmitSubReddits.Items.AddRange(subredditNames.ToArray());
                });
            }

        }

        public void LinkSubmitAddLink_Click()
        {
            List<LinkToSubmit> linksWillSubmit = new List<LinkToSubmit>();
            if (!string.IsNullOrEmpty(Form.LinkSubmitSubredditName.Text))
            {
                LinkToSubmit l = new LinkToSubmit();
                l.SubReddit = Form.LinkSubmitSubredditName.Text;
                l.Title = Form.LinkSubmitTitle.Text;
                l.Url = Form.LinkSubmitUrls.Text;
                linksWillSubmit.Add(l);
            }
            else
            {
                List<string> selectedSubreddits = new List<string>();
                selectedSubreddits = Form.LinkSubmitSubReddits.CheckedItems.OfType<string>().ToList();

                foreach (string s in selectedSubreddits)
                {
                    LinkToSubmit l = new LinkToSubmit();
                    l.SubReddit = s;
                    l.Title = Form.LinkSubmitTitle.Text;
                    l.Url = Form.LinkSubmitUrls.Text;
                    linksWillSubmit.Add(l);
                }
            }
            List<LinkToSubmit> linksAlreadySubmitted = new List<LinkToSubmit>();
            linksAlreadySubmitted = Form.LinkSubmitLinksToSubmit.Rows.ToList().Select(s => s.DataBoundItem).OfType<LinkToSubmit>().ToList();
            linksWillSubmit.AddRange(linksAlreadySubmitted);
            linksWillSubmit = linksWillSubmit.Distinct(new LinkComparer()).ToList();
            Form.LinkSubmitLinksToSubmit.DataSource = linksWillSubmit;
            Form.LinkSubmitTotalLinksToSubmit.Text = Convert.ToString(linksWillSubmit.Count);
            if (Form.LinkSubmitLinksToSubmit.Columns[0].Name != "Submitted")
            {
                Form.LinkSubmitLinksToSubmit.Columns.Add("Submitted");
                Telerik.WinControls.UI.GridViewDataColumn submittedColumn = Form.LinkSubmitLinksToSubmit.Columns["Submitted"];
                Form.LinkSubmitLinksToSubmit.Columns.Remove("Submitted");
                Form.LinkSubmitLinksToSubmit.Columns.Insert(0, submittedColumn);
            }
            
        }

        public void LinkSubmitRemoveSelected_Click()
        {
            if (Form.LinkSubmitLinksToSubmit.SelectedRows.Count() != 0)
            {
                Form.LinkSubmitLinksToSubmit.SelectedRows[0].Delete();
                Form.LinkSubmitTotalLinksToSubmit.Text = Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows.Count);
            }
        }

        public void LinkSubmitRemoveAll_Click()
        {
            Form.LinkSubmitLinksToSubmit.Rows.Clear();
            Form.LinkSubmitTotalLinksToSubmit.Text = Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows.Count);
        }

       

        #endregion

       

        private LinkSubmitWebRequest CreateWebRequest()
        {
            CookieContainer cookies = new CookieContainer();
            DataEntities container2 = new DataEntities();
            List<ProxiesLoaded> proxies = container2.ProxiesLoadeds.ToList();
            List<WebProxy> proxiesload = new List<WebProxy>();
            foreach (ProxiesLoaded p in proxies)
            {
                WebProxy myProxy = new WebProxy();
                Uri newUri = new Uri(string.Format("http://{0}:{1}", p.URL, p.Port));
                myProxy.Address = newUri;
                if (!string.IsNullOrEmpty(p.UserName))
                {
                    myProxy.Credentials = new NetworkCredential(p.UserName, p.Password);
                }
                proxiesload.Add(myProxy);
            }
            Setting s = container2.Settings.Single(setting => setting.Name == "ConnectionTimeout");
            return new LinkSubmitWebRequest(Convert.ToInt32(s.Value),cookies, proxiesload, source.Token, _pauseEvent);
        }

        private RegularWebRequest CreateRegularWebRequest()
        {
            DataEntities container2 = new DataEntities();
            Setting s = container2.Settings.Single(setting => setting.Name == "ConnectionTimeout");
            return new RegularWebRequest(Convert.ToInt32(s.Value));
        }

        private bool SubmitLink(CancellationToken cancelRequested, int whichRow)
        {

            _pauseEvent.WaitOne();
            DataEntities container2 = new DataEntities();
            LinkSubmitWebRequest cw = CreateWebRequest();

            string response = string.Empty;
            string captchaAnswer = null;
            string username = string.Empty; string email = string.Empty; string captchaimage = string.Empty;


            ProxiesLoaded prox = null;

            RedditAccount redditUser = new RedditAccount();
            string userHash = string.Empty;
            NameValueCollection inputs = new NameValueCollection();
            try
            {
                cw.DownloadData("http://www.reddit.com");
                


                redditUser = Form.RetrieveFreeRedditUser();

                
                inputs.Add("passwd", redditUser.Password);
                inputs.Add("rem", "false");
                inputs.Add("user", redditUser.UserName);

                inputs.Add("api_type", "json");


                int proxyID = cw.proxyUsed;
                prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                prox.InUse = true;
                container2.SaveChanges();

                response = Encoding.ASCII.GetString(cw.UploadValues("https://ssl.reddit.com/api/login/" + redditUser.UserName, "POST", inputs));

                prox.InUse = false;
                container2.SaveChanges();


                userHash = JObject.Parse(response)["json"].Last.First.First.Last.ToString();

                proxyID = cw.proxyUsed;
                prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                prox.InUse = true;
                container2.SaveChanges();

                response = cw.UploadString("http://www.reddit.com/api/new_captcha", "renderstyle=html");
                prox.InUse = false;
                container2.SaveChanges();

                captchaimage = JObject.Parse(response)["jquery"].Last.Last.First.ToString();

                proxyID = cw.proxyUsed;
                prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                prox.InUse = true;
                container2.SaveChanges();

                var image = cw.DownloadData(string.Format("http://www.reddit.com/captcha/{0}.png", captchaimage));
                prox.InUse = false;
                container2.SaveChanges();

                Setting captchaUserSetting = container2.Settings.Single(setting => setting.Name == "CaptchaUserName");
                Setting captchaUserPassword = container2.Settings.Single(setting => setting.Name == "CaptchaPassword");
            Setting captchaService = container2.Settings.Single(setting => setting.Name == "CaptchaService");

            if (captchaService.Value == "DeathByCaptcha" || captchaService.Value == "Local Captcha Solver")
            {
                Client client = (Client)new SocketClient(Convert.ToString(captchaUserSetting.Value), Convert.ToString(captchaUserPassword.Value));
                Captcha captcha = null;

                captcha = client.Decode(image, 30);
                captchaAnswer = captcha.Text;
            }
            else if (captchaService.Value == "De-Captcher")
            {
                uint p_pict_to;
                uint p_pict_type;
                uint major_id;
                uint minor_id;
                
                string answer_captcha;
                
                DecaptcherLib.Decaptcher.RecognizePicture("api.decaptcher.com", 3500, Convert.ToString(captchaUserSetting.Value), Convert.ToString(captchaUserPassword.Value), image, out p_pict_to, out p_pict_type, out answer_captcha, out major_id, out minor_id);
                captchaAnswer = answer_captcha;
            }


            }
            catch (System.Exception e2)
            {
                if (prox == null && cw.proxyUsed != 0)
                {
                    int proxyID = cw.proxyUsed;

                    prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                    prox.InUse = false;
                    container2.SaveChanges();
                }


                
                
                if (e2.InnerException != null && e2.InnerException.Message.Contains("cancel"))
                {
                    DecreaseThreadCountLbl(1);

                    return false;
                }
                else if (e2.Message.Contains("CAPTCHA was rejected"))
                {
                    WriteToLog("CAPTCHA was rejected, please check if it's a valid image");
                }
                else if (e2.Message.Contains("balance is too low"))
                {
                    WriteToLog("Captcha service balance too low.");
                    source.Cancel();
                    DecreaseThreadCountLbl(1);
                }
                else if (e2.Message.Contains("Object reference not set to an instance of an object.") || e2.Message.Contains("An exception occurred during a WebClient request."))
                {
                    if (!Form.LinkSubmitContinueWhenNoIP.Checked)
                    {
                        if (ThreadsEnded == false)
                        {
                            WriteToLog("No more free proxies available.  Try again later.");
                            ThreadsEnded = true;
                        }
                        source.Cancel();
                        DecreaseThreadCountLbl(1);
                    }
                    else
                    {
                        //signal for everyone to wait 10 min

                        _pauseEvent.Reset();
                        WriteToLog("Sleeping for 10 minutes");
                        if (Form.InvokeRequired)
                        {
                            Form.Invoke((MethodInvoker)delegate
                            {
                                timer.Enabled = true;
                            });
                        }
                    }
                    return false;
                }
                else
                {
                    
                        WriteToLog("Connection Timed Out");
                        int proxyID = cw.proxyUsed;
                        prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                        prox.NextUse = DateTime.Now.AddMinutes(10);
                        container2.SaveChanges();
                        return false;
                    
                }
                
            }

                if (!string.IsNullOrEmpty(captchaAnswer))
                {

                    inputs = new NameValueCollection();
                    inputs.Add("url", Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[2].Value));
                    inputs.Add("sr", Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[1].Value));
                    inputs.Add("uh", userHash);
                    inputs.Add("kind", "link");
                    inputs.Add("title", Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[3].Value));
                    inputs.Add("iden", captchaimage);
                    inputs.Add("captcha", captchaAnswer);
                    inputs.Add("api_type", "json");


                    try
                    {
                        response = Encoding.ASCII.GetString(cw.UploadValues("https://ssl.reddit.com/api/submit/", "POST", inputs));
                    }
                    catch (System.Exception e3)
                    {
                        if (prox == null)
                        {
                            int proxyID = cw.proxyUsed;
                            prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);

                        }
                        prox.InUse = false;
                        container2.SaveChanges();
                        if (e3.Message.Contains("timed out"))
                        {
                            WriteToLog("Connection Timed Out");
                            int proxyID = cw.proxyUsed;
                            prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                            prox.NextUse = DateTime.Now.AddMinutes(10);
                            container2.SaveChanges();
                            return false;
                        }
                        else if (e3.InnerException != null && e3.InnerException.Message.Contains("cancel"))
                        {

                            DecreaseThreadCountLbl(1);
                            return false;
                        }
                        else if (e3.Message.Contains("Object reference not set to an instance of an object.") || e3.Message.Contains("An exception occurred during a WebClient request."))
                        {
                            if (!Form.LinkSubmitContinueWhenNoIP.Checked)
                            {
                                if (ThreadsEnded == false)
                                {
                                    WriteToLog("No more free proxies available.  Try again later.");
                                    ThreadsEnded = true;
                                }
                                source.Cancel();
                                DecreaseThreadCountLbl(1);
                            }
                            else
                            {
                                //signal for everyone to wait 10 min

                                _pauseEvent.Reset();
                                WriteToLog("Sleeping for 10 minutes");
                                if (Form.InvokeRequired)
                                {
                                    Form.Invoke((MethodInvoker)delegate
                                    {
                                        timer.Enabled = true;
                                    });
                                }
                            }
                            return false;
                        }
                        else
                        {

                            WriteToLog("Connection Timed Out");
                            int proxyID = cw.proxyUsed;
                            prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                            prox.NextUse = DateTime.Now.AddMinutes(10);
                            container2.SaveChanges();
                            return false;

                        }
                        
                    }


                    if (response.Contains("RATELIMIT"))
                    {
                        WriteToLog("Rate Limit reached for ip, marking next use for 10+ min.");
                        int proxyID = cw.proxyUsed;
                        prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                        prox.NextUse = DateTime.Now.AddMinutes(10);
                        container2.SaveChanges();

                        return false;
                    }
                    else if (response.Contains("care to try these again"))
                    {
                        WriteToLog("Invalid Captcha sent to reddit");

                        return false;
                    }
                    else if (response.Contains("NO_LINKS"))
                    {
                        WriteToLog("Subreddit doesn't support links");
                        Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value = "Subreddit doesn't support links";
                        //don't submit this link anymore
                        MarkRecordComplete(whichRow);
                        return false;
                    }
                    else if (response.Contains("ALREADY_SUB"))
                    {
                        WriteToLog("Already submitted this link to this subreddit");
                        if (Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value) != "Submitted")
                        {
                            Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value = "Already submitted this link";
                        }
                        //don't submit this link anymore
                        MarkRecordComplete(whichRow);
                        return false;
                    }
                    else if (response.ToLower().Contains("doesn't exist"))
                    {
                        WriteToLog("Subreddit doesn't exist");
                        //don't submit this link anymore
                        Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value = "Subreddit doesn't exist";
                        MarkRecordComplete(whichRow);
                        return false;
                    }
                    else if (response.ToLower().Contains("you aren't allowed to post there."))
                    {
                        WriteToLog("Subreddit doesn't allow posts");
                        //don't submit this link anymore
                        Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value = "Subreddit doesn't allow posts";
                        MarkRecordComplete(whichRow);
                        return false;
                    }
                    else
                    {
                        WriteToLog("Link Submitted Successfully");
                        Form.LinkSubmitLinksToSubmit.Rows[whichRow].Cells[0].Value = "Submitted";
                    }
                }
            

            return true;


        }

        private Task<bool> SubmitLinkAsync(CancellationToken cancellationToken, int numLinksToSubmit)
        {
            int i = 0;
            bool found = false;
            while (i < numLinksToSubmit && found == false)
            {
                LinkInfo l = records[i];
                if (l.submitted == false)
                {
                    found = true;
                    
                    break;
                }
                i = i + 1;
            }
            if (found == true)
            {

                return Task<bool>.Factory.StartNew(() => SubmitLink(cancellationToken, i)).ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        NumLinksSubmitted++;
                        UpdateProgress(1);
                        MarkRecordComplete(i);

                    }
                    


                    if (cancellationToken.IsCancellationRequested)
                    {
                        DecreaseThreadCountLbl(1);
                    }
                    else
                    {

                        if (NumLinksSubmitted < numLinksToSubmit && NumThreadsRunning > 0)
                        {
                            try
                            {
                                SubmitLinkAsync(source.Token, numLinksToSubmit);
                            }
                            catch (OperationCanceledException)
                            {
                                WriteToLog("Cancelled");
                            }
                        }
                        else if (NumLinksSubmitted == numLinksToSubmit && NumThreadsRunning > 0)
                        {
                            DecreaseThreadCountLbl(1);
                            source.Cancel();
                        }
                        else
                        {
                            DecreaseThreadCountLbl(1);
                            source.Cancel();
                        }

                    }
                    return true;
                });
            }
            else
            {
                DecreaseThreadCountLbl(1);
                
            }
            return new Task<bool>(() => true);
        }

        private void MakeAsyncTasks_SubmitLinks(CancellationToken cancellationToken, int maxThreads, int maxLinksToSubmit)
        {

            for (int i2 = 0; i2 < maxThreads; i2++)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Task<bool> asyncCall = null;
                    
                    asyncCall = SubmitLinkAsync(source.Token, maxLinksToSubmit);
                    asyncCalls.Add(asyncCall);

                }
            }

        }

        public void LinkSubmitStart_Click()
        {
            source = new CancellationTokenSource();
            Form.SubmitLinks.Enabled = false;
            ThreadsEnded = false;

            Form.LinksSubmittedlbl.Text = "0";
            Form.LinkSubmitNumToCreate.Text = Convert.ToString(Form.LinkSubmitLinksToSubmit.Rows.Count);
            int numLinksToSubmit = Convert.ToInt32(Form.LinkSubmitNumToCreate.Text);
            WriteToLog("Link Submitter Started");
            Form.LinkSubmitProgress.Maximum = numLinksToSubmit;
            Form.LinkSubmitProgress.Value1 = 0;
            Form.LinkSubmitThreadsRunning.Text = Form.LinkSubmitNumThreadsCreate.Text;
            NumThreadsRunning = Convert.ToInt32(Form.LinkSubmitNumThreadsCreate.Text);
            NumLinksSubmitted = 0;
            int i = 0;
            records = new Dictionary<int, LinkInfo>();
            foreach (Telerik.WinControls.UI.GridViewRowInfo r in Form.LinkSubmitLinksToSubmit.Rows)
            {
                records.Add(i, new LinkInfo(false, false));
                i=i+1;
            }

            Task.Factory.StartNew(() => MakeAsyncTasks_SubmitLinks(source.Token, NumThreadsRunning, numLinksToSubmit)).ContinueWith(t =>
            {
                Task.WaitAll(asyncCalls.ToArray());
                NumThreadsRunning = 0;
               
                Form.LinkSubmitNumThreadsCreate.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitNumThreadsCreate.Text = "0"; Form.LinkSubmitNumThreadsCreate.Enabled = true; });
                Form.SubmitLinks.BeginInvoke((MethodInvoker)delegate() { Form.SubmitLinks.Enabled = true; ;});
                Form.LinkSubmitThreadsRunning.BeginInvoke((MethodInvoker)delegate() { Form.LinkSubmitThreadsRunning.Text = "0"; });
                _resetEvent.Set();
                
            });




        }

        public void StopSubmitting()
        {

            if (NumThreadsRunning != 0)
            {
                if (_pauseEvent.WaitOne(0) == false)
                {
                    source.Cancel();
                    _pauseEvent.Set();

                    timer.Enabled = false;

                    Form.Cursor = Cursors.AppStarting;
                    _resetEvent.WaitOne();
                }

            }

            WriteToLog("Stopped all threads.");
            Form.SubmitLinks.Enabled = true;
            Form.Cursor = Cursors.Default;

        }

      


        


    }
}
