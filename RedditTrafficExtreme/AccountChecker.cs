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
    public class AccountChecker
    {
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        
       static readonly object _object = new object();
        static readonly object _recordNumberObject = new object();
        List<Task<bool>> asyncCalls = new List<Task<bool>>();
        RedditForm Form = null;

       
        private int numAccountsChecked = 0;
        public int NumAccountsChecked
        {
            get { lock (_object) { return numAccountsChecked; } }
            set { lock (_object) { numAccountsChecked = value; } }
        }

        private bool threadsEnded = false;
        public bool ThreadsEnded
        {
            get { lock (_object) { return threadsEnded; } }
            set { lock (_object) { threadsEnded = value; } }
        }


        private Dictionary<int, bool> records = new Dictionary<int, bool>();
        public void MarkRecordComplete(int recordNumber)
        {
            lock (_recordNumberObject)
            {
                bool l = records[recordNumber];
                
                l = true;
                records[recordNumber] = l;
            }
        }
        

        private int numThreadsRunning = 0;
        public int NumThreadsRunning
        {
            get { lock (_object) { return numThreadsRunning; } }
            set { lock (_object) { numThreadsRunning = value; } }
        }

        public AccountChecker(RedditForm form)
        {
            Form = form;
           
        }

        private void WriteToLog(string logText)
        {
            if (Form.ProcessLog.InvokeRequired)
            {
                Form.ProcessLog.BeginInvoke((MethodInvoker)delegate() { Form.ProcessLog.Text = logText + Environment.NewLine + Form.ProcessLog.Text; ;});
            }
            else
            {
                Form.ProcessLog.Text = logText + Environment.NewLine + Form.ProcessLog.Text;
            }
        }

        private void UpdateProgress(int i)
        {

            if (Form.AccountCheckerProgress.InvokeRequired)
            {
                Form.AccountCheckerProgress.BeginInvoke((MethodInvoker)delegate() { if (Form.AccountCheckerProgress.Value1 <= Form.ExistingAccounts.Items.Count) { Form.AccountCheckerProgress.Value1 = Form.AccountCheckerProgress.Value1 + i; } });
            }
            else
            {
                if (Form.AccountCheckerProgress.Value1 <= Form.ExistingAccounts.Items.Count)
                {
                    Form.AccountCheckerProgress.Value1 = Form.AccountCheckerProgress.Value1 + i;
                }
            }

            if (Form.AccountCheckerProgressNumChecked.InvokeRequired)
            {
                Form.AccountCheckerProgressNumChecked.BeginInvoke((MethodInvoker)delegate() { if (Form.AccountCheckerProgress.Value1 <= Form.ExistingAccounts.Items.Count) { Form.AccountCheckerProgressNumChecked.Text = Convert.ToString(Convert.ToInt32(Form.AccountCheckerProgressNumChecked.Text) + i); } });
            }
            else
            {
                if (Form.AccountCheckerProgress.Value1 <= Form.ExistingAccounts.Items.Count)
                {
                    Form.AccountCheckerProgressNumChecked.Text = Convert.ToString(Convert.ToInt32(Form.AccountCheckerProgressNumChecked.Text) + i);
                }
            }

        }

        private void DecreaseThreadCountLbl(int i)
        {
            if (NumThreadsRunning != 0)
            {
                if (Form.AccountCheckerNumThreadsRunning.InvokeRequired)
                {
                    Form.AccountCheckerNumThreadsRunning.BeginInvoke((MethodInvoker)delegate() { Form.AccountCheckerNumThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.AccountCheckerNumThreadsRunning.Text) - 1); ;});
                }
                else
                {
                    Form.AccountCheckerNumThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.AccountCheckerNumThreadsRunning.Text) - 1);
                }
                NumThreadsRunning = NumThreadsRunning - 1;
            }
            if (NumThreadsRunning == 0)
            {
                Form.AccountCheckerProgress.BeginInvoke((MethodInvoker)delegate()
                {
                    if (Form.AccountCheckerProgress.Value1 != Form.AccountCheckerProgress.Maximum)
                    {
                        if (ThreadsEnded == false)
                        {
                            WriteToLog("No Threads running but not all accounts checked!");
                            ThreadsEnded = true;
                        }
                    }
                });
                Form.AccountCheckerNumThreadsToCreate.BeginInvoke((MethodInvoker)delegate() { Form.AccountCheckerNumThreadsToCreate.Text = "0"; Form.AccountCheckerNumThreadsToCreate.Enabled = true; });
                Form.CheckDisabledAccounts.BeginInvoke((MethodInvoker)delegate() { Form.CheckDisabledAccounts.Enabled = true; ;});

                _resetEvent.Set();
            }

        }

       
        private RegularProxyWebRequest CreateWebRequest()
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

                myProxy.Credentials = new NetworkCredential(p.UserName, p.Password);
                proxiesload.Add(myProxy);
            }
            Setting s = container2.Settings.Single(setting => setting.Name == "ConnectionTimeout");

            return new RegularProxyWebRequest(Convert.ToInt32(s.Value), cookies, proxiesload);
        }

        

        private bool CheckAccount(int whichRow)
        {

            
            DataEntities container2 = new DataEntities();
            RegularProxyWebRequest cw = CreateWebRequest();

            string response = string.Empty;
            
            string username = string.Empty; string email = string.Empty;


            ProxiesLoaded prox = null;

            
            string userHash = string.Empty;
            NameValueCollection inputs = new NameValueCollection();
            try
            {
                cw.DownloadData("http://www.reddit.com");
                
                int ID = Convert.ToInt32(Form.ExistingAccounts.Items[whichRow].Value);
                RedditAccount foundRedditAccount = container2.RedditAccounts.Single(s => s.ID == ID);



                inputs.Add("passwd", foundRedditAccount.Password);
                inputs.Add("rem", "false");
                inputs.Add("user", foundRedditAccount.UserName);

                inputs.Add("api_type", "json");


                int proxyID = cw.proxyUsed;
                prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                prox.InUse = true;
                container2.SaveChanges();

                response = Encoding.ASCII.GetString(cw.UploadValues("https://ssl.reddit.com/api/login/" + foundRedditAccount.UserName, "POST", inputs));

                prox.InUse = false;
                container2.SaveChanges();


                userHash = JObject.Parse(response)["json"].Last.First.First.Last.ToString();
               
                

                
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


                if (e2.Message.Contains("timed out"))
                {
                    WriteToLog("Connection Timed Out");
                    int proxyID = cw.proxyUsed;
                    prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                    prox.NextUse = DateTime.Now.AddMinutes(10);
                    container2.SaveChanges();
                    return false;
                }
                else if (e2.InnerException != null && e2.InnerException.Message.Contains("cancel"))
                {
                    DecreaseThreadCountLbl(1);

                    return false;
                }
                else if (e2.Message.Contains("Object reference not set to an instance of an object.") || e2.Message.Contains("An exception occurred during a WebClient request."))
                {
                    
                        if (ThreadsEnded == false)
                        {
                            WriteToLog("No more free proxies available.  Try again later.");
                            ThreadsEnded = true;
                        }
                        
                        DecreaseThreadCountLbl(1);
                    
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

            return true;

        }

        private Task<bool> CheckAccountAsync(int numAccountsToCheck)
        {
            int i = 0;
            bool found = false;
            while (i < numAccountsToCheck && found == false)
            {
                bool l = records[i];
                if (l == false)
                {
                    found = true;
                    
                    break;
                }
                i = i + 1;
            }
            if (found == true)
            {

                return Task<bool>.Factory.StartNew(() => CheckAccount(i)).ContinueWith(t =>
                {
                    if (t.Result && records[i] == false)
                    {
                        NumAccountsChecked++;
                        UpdateProgress(1);
                        MarkRecordComplete(i);
                        //update database with account=active
                        int ID = Convert.ToInt32(Form.ExistingAccounts.Items[i].Value);
                        DataEntities container2 = new DataEntities();
                        RedditAccount foundRedditAccount = container2.RedditAccounts.Single(s => s.ID == ID);
                        foundRedditAccount.Active = true;
                        container2.SaveChanges();
                    }
                    


                   
                        if (NumAccountsChecked < numAccountsToCheck && NumThreadsRunning > 0)
                        {
                           
                                CheckAccountAsync(numAccountsToCheck);
                          
                        }
                        else if (NumAccountsChecked == numAccountsToCheck && NumThreadsRunning > 0)
                        {
                            DecreaseThreadCountLbl(1);
                           
                        }
                        else
                        {
                            DecreaseThreadCountLbl(1);
                            
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

        private void MakeAsyncTasks_CheckAccounts(int maxThreads, int maxAccountsToCheck)
        {

            for (int i2 = 0; i2 < maxThreads; i2++)
            {
                
                    Task<bool> asyncCall = null;

                    asyncCall = CheckAccountAsync(maxAccountsToCheck);
                    asyncCalls.Add(asyncCall);

                
            }

        }

        public void AccountCheckerStart_Click()
        {
            
            Form.CheckDisabledAccounts.Enabled = false;

            DataEntities container2 = new DataEntities();
            foreach (RedditAccount r in container2.RedditAccounts.ToList())
            {
                r.Active = false;

            }
            container2.SaveChanges();
            ThreadsEnded = false;
            Form.DataBindExistingAcounts();
            Form.AccountCheckerProgressNumChecked.Text = "0";
            Form.AccountCheckerProgressNumToCheck.Text = Convert.ToString(Form.ExistingAccounts.Items.Count);
            int numAccountsToCheck = Convert.ToInt32(Form.AccountCheckerProgressNumToCheck.Text);
            WriteToLog("Account Checker Started");
            Form.AccountCheckerProgress.Maximum = numAccountsToCheck;
            Form.AccountCheckerProgress.Value1 = 0;
            Form.AccountCheckerNumThreadsRunning.Text = Form.AccountCheckerNumThreadsToCreate.Text;
            NumThreadsRunning = Convert.ToInt32(Form.AccountCheckerNumThreadsToCreate.Text);
            NumAccountsChecked = 0;
            int i = 0;
            records = new Dictionary<int, bool>();
            foreach (Telerik.WinControls.UI.ListViewDataItem r in Form.ExistingAccounts.Items)
            {
                records.Add(i, false);
                i=i+1;
            }

            Task.Factory.StartNew(() => MakeAsyncTasks_CheckAccounts(NumThreadsRunning, numAccountsToCheck)).ContinueWith(t =>
            {
                Task.WaitAll(asyncCalls.ToArray());
                Form.DataBindExistingAcounts();
            });




        }


    }
}
