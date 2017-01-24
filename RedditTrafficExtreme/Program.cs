using Rhino.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RedditTrafficExtreme
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var publicKey = File.ReadAllText("publicKey.xml");

          //  new LicenseValidator(publicKey, "license.xml")
             // .AssertValidLicense();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RedditForm());
        }
    }
}
