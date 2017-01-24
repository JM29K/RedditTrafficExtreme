using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Net;
using DeathByCaptcha;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Threading;
using HtmlAgilityPack;
using Telerik.WinControls.UI;
using System.Threading.Tasks;

namespace RedditTrafficExtreme
{
    public partial class RedditForm : Telerik.WinControls.UI.RadForm
    {
        #region global
        
        DataEntities container = null;

        CreateAccounts createAccountModule = null;
        LinkSubmitter linkSubmitModule = null;
        AccountChecker accountCheckerModule = null;
        
        static RedditForm() 
        { 
            Telerik.WinControls.RadTypeResolver.Instance.ResolveTypesInCurrentAssembly = true; 
        } 

        public RedditForm()
        {
            InitializeComponent();
            container = new DataEntities();
            DataBindExistingAcounts();
            if (container.Settings.ToList().Count() != 4)
            {
                foreach (Setting set in container.Settings.ToList())
                {
                    container.Settings.Remove(set);
                }
                container.SaveChanges();

                Setting newSetting = new Setting();
                newSetting.Name = "ConnectionTimeout";
                Setting newSetting2 = new Setting();
                newSetting2.Name = "CaptchaService";
                Setting newSetting3 = new Setting();
                newSetting3.Name = "CaptchaUserName";
                Setting newSetting4 = new Setting();
                newSetting4.Name = "CaptchaPassword";
                container.Settings.Add(newSetting);
                container.Settings.Add(newSetting2);
                container.Settings.Add(newSetting3);
                container.Settings.Add(newSetting4);
                container.SaveChanges();
            }
            radPageView1.SelectedPage = radPageViewPage1;
            accountCheckerModule = new AccountChecker(this);
            foreach (ProxiesLoaded p in container.ProxiesLoadeds.ToList())
            {
                p.InUse = false;

            }
            container.SaveChanges();
        }

        private void LinkSubmitterCommonFilter_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            linkSubmitModule.BindSubRedditList();
        }

        private void LinkSubmitSubredditFilter_TextChanged(object sender, EventArgs e)
        {
            linkSubmitModule.LinkSubmitSubredditFilter_TextChanged();
        }

        private void LinkSubmitStart_Click(object sender, EventArgs e)
        {
            linkSubmitModule.LinkSubmitStart_Click();
        }

        private void LinkSubmitRemoveAll_Click(object sender, EventArgs e)
        {
            linkSubmitModule.LinkSubmitRemoveAll_Click();
        }

        private void LinkSubmitRemoveSelected_Click(object sender, EventArgs e)
        {
            linkSubmitModule.LinkSubmitRemoveSelected_Click();
        }

        private void LinkSubmitAddLink_Click(object sender, EventArgs e)
        {
            linkSubmitModule.LinkSubmitAddLink_Click();
            
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {
            switch (radPageView1.SelectedPage.Text)
            {
                case "About":
                    AboutRedditTrafficExtreme about = new AboutRedditTrafficExtreme();
                    about.Show();
                    about.TopMost = true;
                    break;
                case "Link Submitter":
                    if (linkSubmitModule == null)
                        linkSubmitModule = new LinkSubmitter(this);
                    if(LinkSubmitSubReddits.Items.Count == 0)
                        linkSubmitModule.BindSubRedditList();
                    break;
                case "Account Creator":
                    if (createAccountModule == null)
                        createAccountModule = new CreateAccounts(this);
                    ToolTip toolTip1 = new ToolTip();

                     toolTip1.AutoPopDelay = 5000;
                     toolTip1.InitialDelay = 1000;
                     toolTip1.ReshowDelay = 500;
                     
                     toolTip1.SetToolTip(CreateAccountsContinueCreate, "Threads will sleep for 10 minutes and continue trying again");
                     
                    break;
                case "Account Manager":
                    if (accountCheckerModule == null)
                        accountCheckerModule = new AccountChecker(this);
                    break;
                case "Settings":
                    
                    foreach (Setting s in container.Settings.ToList())
                    {
                        if (s.Name == "ConnectionTimeout")
                        {
                            SettingsConnectionTimeout.Text = s.Value;
                        }
                        if (s.Name == "CaptchaService")
                        {
                            SettingsCaptchaService.SelectedValue = s.Value;
                        }
                        if (s.Name == "CaptchaUserName")
                        {
                            SettingsCaptchaUserName.Text = s.Value;
                        }
                        if (s.Name == "CaptchaPassword")
                        {
                            SettingsCaptchaPassword.Text = s.Value;
                        }
                    }
                    break;

            }
           
        }

        public RedditAccount RetrieveFreeRedditUser()
        {
            DataEntities modelcontainer = new DataEntities();
            bool goodUser = true;
            int i = 0;
            Random rand = new Random();
            RedditAccount redditAccountChose = new RedditAccount();

            List<RedditAccount> accounts = modelcontainer.RedditAccounts.ToList();
            do
            {
                if (i == (accounts.Count * 2))
                {
                    return null;
                }
                int chosenAccount = rand.Next(0, accounts.Count - 1);
                redditAccountChose = accounts[chosenAccount];

                if ((!redditAccountChose.InUse || i == 0))
                {
                    return redditAccountChose;


                }
                else
                {
                    goodUser = false;
                }
                i++;
            }
            while (goodUser == false);
            return redditAccountChose;
        }
        #endregion

        #region AccountManager
        public void DataBindExistingAcounts()
        {
            if (ExistingAccounts.InvokeRequired)
            {
                LinkSubmitProcessLog.BeginInvoke((MethodInvoker)delegate() {
                    ExistingAccounts.DataSource = from l in container.RedditAccounts.ToList()
                                                  select new { ID = l.ID, display = string.Format("UserName: {0} - Active: {1}", l.UserName, l.Active) };
                    ExistingAccounts.DisplayMember = "display";
                    ExistingAccounts.ValueMember = "ID";
                    NumAccountsLoaded.Text = Convert.ToString(container.RedditAccounts.Count());
                    NumProxiesLoaded.Text = Convert.ToString(container.ProxiesLoadeds.Count());
                });
            }
            else
            {
                ExistingAccounts.DataSource = from l in container.RedditAccounts.ToList()
                                              select new { ID = l.ID, display = string.Format("UserName: {0} - Active: {1}", l.UserName, l.Active) };
                ExistingAccounts.DisplayMember = "display";
                ExistingAccounts.ValueMember = "ID";
                NumAccountsLoaded.Text = Convert.ToString(container.RedditAccounts.Count());
                NumProxiesLoaded.Text = Convert.ToString(container.ProxiesLoadeds.Count());
            }
            
        }

        private void AddAccountSubmit_Click(object sender, EventArgs e)
        {

            RedditAccount newRedditAccount = new RedditAccount();
            ProxiesLoaded newProxy = new ProxiesLoaded();
            newRedditAccount.UserName = AddAccountRedditUserName.Text;
            newRedditAccount.Password = AddAccountRedditPassword.Text;
            newRedditAccount.InUse = false;
            

            container.RedditAccounts.Add(newRedditAccount);
            container.SaveChanges();
            ProcessLog.Text = "Account Added." + Environment.NewLine + ProcessLog.Text;
            DataBindExistingAcounts();
        }

        private void LoadAccounts_Click(object sender, EventArgs e)
        {

            RedditAccount newRedditAccount = new RedditAccount();
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open comma separated account file (username,password)";
            fDialog.Filter = "TXT Files|*.txt";
            fDialog.InitialDirectory = @"C:\";
            string readLine = string.Empty;
            string[] splitLine;
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader w = File.OpenText(fDialog.FileName))
                {
                    while ((readLine = w.ReadLine()) != null)
                    {
                        splitLine = readLine.Split(',');
                        newRedditAccount.UserName = splitLine[0];
                        newRedditAccount.Password = splitLine[1];
                        container.RedditAccounts.Add(newRedditAccount);
                        container.SaveChanges();
                    }
                }
                ProcessLog.Text = "Accounts successfully loaded!" + Environment.NewLine + ProcessLog.Text;
                DataBindExistingAcounts();
            }
        }

        private void LoadProxies_Click(object sender, EventArgs e)
        {

            ProxiesLoaded newProxies = new ProxiesLoaded();
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open comma separated proxy file (URL,Port,UserName,Password)";
            fDialog.Filter = "TXT Files|*.txt";
            fDialog.InitialDirectory = @"C:\";
            string readLine = string.Empty;
            string[] splitLine;
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader w = File.OpenText(fDialog.FileName))
                {
                    while ((readLine = w.ReadLine()) != null)
                    {
                        splitLine = readLine.Split(',');
                        newProxies.URL = splitLine[0];
                        newProxies.Port = splitLine[1];
                        newProxies.UserName = splitLine[2];
                        newProxies.Password = splitLine[3];
                        newProxies.NextUse = null;
                        container.ProxiesLoadeds.Add(newProxies);
                        container.SaveChanges();
                    }
                }
                ProcessLog.Text = "Proxies successfully loaded!" + Environment.NewLine + ProcessLog.Text;
                DataBindExistingAcounts();
            }
        }

        private void ExportAccounts_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.FileDialog fDialog = new SaveFileDialog();
            fDialog.Title = "Location to save accounts";
            fDialog.Filter = "TXT Files|*.txt";
            fDialog.InitialDirectory = @"C:\";
            if (fDialog.ShowDialog() == DialogResult.OK)
            {

                using (StreamWriter w = File.AppendText(fDialog.FileName))
                {
                    foreach (RedditAccount r in container.RedditAccounts.ToList())
                    {

                        w.WriteLine(string.Format("{0},{1}", r.UserName, r.Password));

                    }
                }
                ProcessLog.Text = "Accounts successfully exported!" + Environment.NewLine + ProcessLog.Text;
            }
        }

        private void ExportProxies_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.FileDialog fDialog = new SaveFileDialog();
            fDialog.Title = "Location to save proxies";
            fDialog.Filter = "TXT Files|*.txt";
            fDialog.InitialDirectory = @"C:\";
            if (fDialog.ShowDialog() == DialogResult.OK)
            {

                using (StreamWriter w = File.AppendText(fDialog.FileName))
                {
                    foreach (ProxiesLoaded r in container.ProxiesLoadeds.ToList())
                    {

                        w.WriteLine(string.Format("{0},{1},{2},{3}", r.URL, r.Port, r.UserName, r.Password));

                    }
                }
                ProcessLog.Text = "Proxies successfully exported!" + Environment.NewLine + ProcessLog.Text;
            }
        }

        private void RemoveSelectedAccount_Click(object sender, EventArgs e)
        {
            int selectedID = Convert.ToInt32(ExistingAccounts.SelectedItem.Value);
            RedditAccount selectedAccount = container.RedditAccounts.Where(s => s.ID == selectedID).Single();
            container.RedditAccounts.Remove(selectedAccount);
            container.SaveChanges();
            ProcessLog.Text = "Account successfully removed" + Environment.NewLine + ProcessLog.Text;
            DataBindExistingAcounts();
        }

        private void RemoveAllAccounts_Click(object sender, EventArgs e)
        {
            List<RedditAccount> redditaccounts = container.RedditAccounts.ToList();

            foreach (RedditAccount r in redditaccounts)
            {
                container.RedditAccounts.Remove(r);
            }
            container.SaveChanges();
            ProcessLog.Text = "All accounts successfully removed" + Environment.NewLine + ProcessLog.Text;
            DataBindExistingAcounts();
        }

        private void RemoveAllProxies_Click(object sender, EventArgs e)
        {
            List<ProxiesLoaded> proxies = container.ProxiesLoadeds.ToList();

            foreach (ProxiesLoaded p in proxies)
            {
                container.ProxiesLoadeds.Remove(p);

            }
            container.SaveChanges();
            ProcessLog.Text = "All proxies successfully removed" + Environment.NewLine + ProcessLog.Text;
            DataBindExistingAcounts();
        }

        private void CheckDisabledAccounts_Click(object sender, EventArgs e)
        {
            accountCheckerModule.AccountCheckerStart_Click();

        }

        private void RemoveDisabledAccounts_Click(object sender, EventArgs e)
        {
            List<RedditAccount> redditaccounts = container.RedditAccounts.Where(s => s.Active == false).ToList();

            foreach (RedditAccount r in redditaccounts)
            {
                container.RedditAccounts.Remove(r);
            }
            container.SaveChanges();
            ProcessLog.Text = "All disabled accounts successfully removed" + Environment.NewLine + ProcessLog.Text;
            DataBindExistingAcounts();
        }

        #endregion

       
       
        private void CreateAccountsStop_Click(object sender, EventArgs e)
        {
            createAccountModule.StopCreating();
        }


        private void CreateAccounts_Click(object sender, EventArgs e)
        {
            createAccountModule.BeginCreating();
        }

        private void SettingsSaveButton_Click(object sender, EventArgs e)
        {
            Setting s = container.Settings.Single(setting => setting.Name == "ConnectionTimeout");
            s.Value = SettingsConnectionTimeout.Text;
            s = container.Settings.Single(setting => setting.Name == "CaptchaService");
            s.Value = Convert.ToString(SettingsCaptchaService.SelectedItem.Text);
            s = container.Settings.Single(setting => setting.Name == "CaptchaUserName");
            s.Value = SettingsCaptchaUserName.Text;
            s = container.Settings.Single(setting => setting.Name == "CaptchaPassword");
            s.Value = SettingsCaptchaPassword.Text;
            container.SaveChanges();
        }

        private void About_Click(object sender, EventArgs e)
        {
           
        }

        
       


    }
}