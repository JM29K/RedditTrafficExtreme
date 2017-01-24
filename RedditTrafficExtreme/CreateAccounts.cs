using DeathByCaptcha;
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
    public class CreateAccounts
    {
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        CancellationTokenSource source = new CancellationTokenSource();
        private System.Timers.Timer timer = new System.Timers.Timer();
        static readonly object _object = new object();
        List<Task<bool>> asyncCalls = new List<Task<bool>>();
        RedditForm Form = null;


        private int numAccountsCreated = 0;
        public int NumAccountsCreated
        {
            get { lock (_object) { return numAccountsCreated; } }
            set { lock (_object) { numAccountsCreated = value; } }
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

        public CreateAccounts(RedditForm form)
        {
            Form = form;
            timer.Enabled = false;
            timer.Interval = 60000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer1_Tick);
        }

        private void WriteToLog(string logText)
        {
            if (Form.CreateAccountsProcessLog.InvokeRequired)
            {
                Form.CreateAccountsProcessLog.BeginInvoke((MethodInvoker)delegate() { Form.CreateAccountsProcessLog.Text = logText + Environment.NewLine + Form.CreateAccountsProcessLog.Text; ;});
            }
            else
            {
                Form.CreateAccountsProcessLog.Text = logText + Environment.NewLine + Form.CreateAccountsProcessLog.Text;
            }
        }

        private void UpdateProgress(int i)
        {

            if (Form.CreateAccountsProgress.InvokeRequired)
            {
                Form.CreateAccountsProgress.BeginInvoke((MethodInvoker)delegate() { if (Form.CreateAccountsProgress.Value1 != Convert.ToInt32(Form.CreateAccountsNumToCreate.Text)) { Form.CreateAccountsProgress.Value1 = Form.CreateAccountsProgress.Value1 + i; } });
            }
            else
            {
                if (Form.CreateAccountsProgress.Value1 != Convert.ToInt32(Form.CreateAccountsNumToCreate.Text))
                {
                    Form.CreateAccountsProgress.Value1 = Form.CreateAccountsProgress.Value1 = Form.CreateAccountsProgress.Value1 + i;
                }
            }

            if (Form.AccountsCreatedlbl.InvokeRequired)
            {
                Form.AccountsCreatedlbl.BeginInvoke((MethodInvoker)delegate() { Form.AccountsCreatedlbl.Text = Convert.ToString(Convert.ToInt32(Form.AccountsCreatedlbl.Text) + i); });
            }
            else
            {
                Form.AccountsCreatedlbl.Text = Convert.ToString(Convert.ToInt32(Form.AccountsCreatedlbl.Text) + i);
            }

        }

        private void DecreaseThreadCountLbl(int i)
        {
            if (NumThreadsRunning != 0)
            {
                if (Form.lblThreadsRunning.InvokeRequired)
                {
                    Form.lblThreadsRunning.BeginInvoke((MethodInvoker)delegate() { Form.lblThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.lblThreadsRunning.Text) - 1); ;});
                }
                else
                {
                    Form.lblThreadsRunning.Text = Convert.ToString(Convert.ToInt32(Form.lblThreadsRunning.Text) - 1);
                }
                NumThreadsRunning = NumThreadsRunning - 1;
            }
            if (NumThreadsRunning == 0)
            {
                Form.CreateAccountsProgress.BeginInvoke((MethodInvoker)delegate()
                {
                    if (Form.CreateAccountsProgress.Value1 != Form.CreateAccountsProgress.Maximum)
                    {
                        if (ThreadsEnded == false)
                        {
                            WriteToLog("No Threads running but not all accounts created!");
                            ThreadsEnded = true;
                        }
                    }
                });
                Form.CreateAccountsNumThreadsCreate.BeginInvoke((MethodInvoker)delegate() { Form.CreateAccountsNumThreadsCreate.Text = "0"; Form.CreateAccountsNumThreadsCreate.Enabled = true; });
                Form.CreateAccountsNumToCreate.BeginInvoke((MethodInvoker)delegate() { Form.CreateAccountsNumToCreate.Text = "0"; Form.CreateAccountsNumToCreate.Enabled = true; });
                Form.CreateAccounts.BeginInvoke((MethodInvoker)delegate() { Form.CreateAccounts.Enabled = true; ;});

                _resetEvent.Set();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _pauseEvent.Set();
            timer.Enabled = false;
        }

        #region AccountCreator
        private string GenerateUserName()
        {
            List<char> randUserLetters = new List<char>();
            for (int i = 0; i < 10; i++)
            {
                char choose;
                int lett = new Random().Next(97, 122);
                choose = (char)lett;
                randUserLetters.Add(choose);
            }
            string username = string.Format("{0}{1}", string.Concat(randUserLetters), new Random().Next(100, 5000));
            return username;
        }

        private CreateAccountsWebRequest CreateWebRequest()
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
            return new CreateAccountsWebRequest(Convert.ToInt32(s.Value), cookies, proxiesload, source.Token, _pauseEvent);
        }

        private bool CreateAccount(CancellationToken cancelRequested)
        {

            _pauseEvent.WaitOne();
            DataEntities container2 = new DataEntities();
            CreateAccountsWebRequest cw = CreateWebRequest();

            string response = string.Empty;
            string captchaAnswer = null;
            string username = string.Empty; string email = string.Empty; string captchaimage = string.Empty;


            ProxiesLoaded prox = null;
            try
            {
                cw.DownloadData("http://www.reddit.com");

                int proxyID = cw.proxyUsed;
                prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                prox.InUse = true;
                container2.SaveChanges();
                response = cw.UploadString("http://www.reddit.com/api/new_captcha", "renderstyle=html");


                captchaimage = JObject.Parse(response)["jquery"].Last.Last.First.ToString();

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

                username = GenerateUserName();
                email = string.Format("{0}@yahoo.com", username);
                
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
                    if (!Form.CreateAccountsContinueCreate.Checked)
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

                NameValueCollection inputs = new NameValueCollection();
                inputs.Add("op", "reg");
                inputs.Add("user", username);
                inputs.Add("email", email);
                inputs.Add("passwd", "test123");
                inputs.Add("passwd2", "test123");
                inputs.Add("iden", captchaimage);
                inputs.Add("captcha", captchaAnswer);
                inputs.Add("api_type", "json");

                try
                {
                    int proxyID = cw.proxyUsed;
                    prox = container2.ProxiesLoadeds.ToList().Single(s => s.ID == proxyID);
                    prox.InUse = true;
                    container2.SaveChanges();
                    response = Encoding.ASCII.GetString(cw.UploadValues("https://ssl.reddit.com/api/register/" + username, "POST", inputs));

                    prox.InUse = false;
                    container2.SaveChanges();
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
                        if (!Form.CreateAccountsContinueCreate.Checked)
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
                else
                {

                    RedditAccount r = new RedditAccount();
                    r.Active = true;
                    r.Password = "test123";
                    r.UserName = username;
                    container2.RedditAccounts.Add(r);
                    container2.SaveChanges();
                    WriteToLog("Account successfully created");

                    return true;
                }
            }
            else
            {
                WriteToLog("Captcha Service could not solve");

                return false;
            }


        }

        private Task<bool> CreateAccountAsync(CancellationToken cancellationToken, int numAccountsToCreate)
        {

            return Task<bool>.Factory.StartNew(() => CreateAccount(cancellationToken)).ContinueWith(t =>
            {
                if (t.Result)
                {
                    NumAccountsCreated++;
                    UpdateProgress(1);


                }


                if (cancellationToken.IsCancellationRequested)
                {
                    DecreaseThreadCountLbl(1);
                }
                else
                {

                    if (NumAccountsCreated < numAccountsToCreate && NumThreadsRunning > 0)
                    {
                        try
                        {
                            CreateAccountAsync(source.Token, numAccountsToCreate);
                        }
                        catch (OperationCanceledException)
                        {
                            WriteToLog("Cancelled");
                        }
                    }
                    else if (NumAccountsCreated == numAccountsToCreate && NumThreadsRunning > 0)
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

        private void MakeAsyncTasks_CreateAccount(CancellationToken cancellationToken, int maxThreads, int maxAccountsToCreate)
        {

            for (int i2 = 0; i2 < maxThreads; i2++)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Task<bool> asyncCall = null;

                    asyncCall = CreateAccountAsync(source.Token, maxAccountsToCreate);
                    asyncCalls.Add(asyncCall);

                }
            }

        }

        public void BeginCreating()
        {
            source = new CancellationTokenSource();
            Form.CreateAccounts.Enabled = false;
            int numAccountsToCreate = Convert.ToInt32(Form.CreateAccountsNumToCreate.Text);
            ThreadsEnded = false;
            Form.AccountsCreatedlbl.Text = "0";
            Form.AccountsToCreatelbl.Text = Form.CreateAccountsNumToCreate.Text;
            WriteToLog("Account Creator Started");
            Form.CreateAccountsProgress.Maximum = numAccountsToCreate;
            Form.CreateAccountsProgress.Value1 = 0;
            Form.lblThreadsRunning.Text = Form.CreateAccountsNumThreadsCreate.Text;
            NumThreadsRunning = Convert.ToInt32(Form.CreateAccountsNumThreadsCreate.Text);
            NumAccountsCreated = 0;
            Task.Factory.StartNew(() => MakeAsyncTasks_CreateAccount(source.Token, NumThreadsRunning, numAccountsToCreate)).ContinueWith(t =>
            {
                Task.WaitAll(asyncCalls.ToArray());
            });




        }

        public void StopCreating()
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
            Form.CreateAccounts.Enabled = true;
            Form.Cursor = Cursors.Default;

        }

        #endregion


        


    }
}
