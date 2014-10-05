using System;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using BMRFME.Whitelist.Api;

namespace BMRFME.Whitelist
{
    public class Program
    {

        public static string ConfigName;
        public static string PluginConfig;

        public static void Main(string[] args)
        {
            try
            {
                var workThread = new Thread(Whitelister.Instance.Start);
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs eventArgs)
                {
                    Whitelister.Instance.Stop();
                    eventArgs.Cancel = false;
                };
                workThread.Start();
                Whitelister.Instance.Logger.Level = Logger.LogLevel.Info;
                //HostApi();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled Exception.");
                while (e != null)
                {
                    Console.WriteLine(e);
                    e = e.InnerException;
                }
            }
        }

        public static void HostApi()
        {
            var baseAddress = new Uri("http://bmrf.me:8081/WhitelisterAPI");
            WebServiceHost serviceHost;
            //WSHttpBinding binding = new WSHttpBinding();
            //BasicHttpBinding binding = new BasicHttpBinding();
            var binding = new WebHttpBinding();

            binding.Security.Mode = WebHttpSecurityMode.Transport;
            serviceHost = new WebServiceHost(typeof(ApiService), baseAddress);

            //serviceHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
            //serviceHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new CredentialValidator();
            //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            //ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            var whb = new WebHttpBehavior();
            var debug = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();

            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                serviceHost.Description.Behaviors.Add(
                    new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            //whb.HttpGetEnabled = true;
            //whb.HttpsGetEnabled = true;
            //serviceHost.Description.Behaviors.Add(whb);

            serviceHost.Open();
        }
    }
}
