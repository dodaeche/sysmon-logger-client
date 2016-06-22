using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;

namespace SysMonLogger
{
    /// <summary>
    /// Main service functionality
    /// </summary>
    public partial class SmlService : ServiceBase
    {
        #region Member Variables
        private Configuration config;
        private System.Timers.Timer timer;
        private ExtendedHttpClient ehc;
        private string remoteUrl;
        private static readonly object locker = new object();
        private List<string> data = new List<string>();
        private string sendData = string.Empty;
        private TraceEventSession tes;
        private bool sendError;
        #endregion

        #region Constructor
        public SmlService()
        {
            InitializeComponent();

            this.config = new Configuration();
            this.ehc = new ExtendedHttpClient();
            this.ehc.Error += OnEhc_Error;
            this.timer = new System.Timers.Timer();
            this.timer.Interval = 5000;
            this.timer.Elapsed += Timer_Elapsed;
        }
        #endregion

        #region Service Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            StartService();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        {
            EventLog.WriteEntry(Global.DISPLAY_NAME, "Service stopping", EventLogEntryType.Information);
            this.timer.Enabled = false;
            this.tes.Source.StopProcessing();
        }
        #endregion

        /// <summary>
        /// Method to start service functions. This provides a public interface so 
        /// that the functionality can be run without initialising a service e.g. debug
        /// </summary>
        public void StartService()
        {
            if (EventLog.SourceExists(Global.DISPLAY_NAME) == false)
            {
                EventLog.CreateEventSource(Global.DISPLAY_NAME, "Application");
            }
            
            EventLog.WriteEntry(Global.DISPLAY_NAME, "Service starting", EventLogEntryType.Information);

            if (ValidateConfig() == false)
            {
                this.ExitCode = -1;
                this.Stop();
                return;
            }

            if (LoadX509Certificate() == false)
            {
                this.ExitCode = -1;
                this.Stop();
                return;
            }

            this.sendError = false;
            this.remoteUrl = "https://" + config.RemoteServer + "/" + Environment.UserDomainName + "/" + Environment.MachineName;
            this.timer.Enabled = true;

            this.tes = new TraceEventSession("SML");
            tes.EnableProvider(new Guid("5770385F-C22A-43E0-BF4C-06F5698FFBD9"), TraceEventLevel.Always);
            tes.Source.Dynamic.All += delegate (TraceEvent te)
            {
                lock (locker)
                {
                    data.Add(te.ToString());
                }
            };

            // Get an initial set of data using a Task so the service OnStart 
            // method can complete without waiting for the process to finish            
            Task.Run(() => { tes.Source.Process(); });
        }

        /// <summary>
        /// 
        /// </summary>
        private bool ValidateConfig()
        {
            string err = this.config.Load();
            if (err.Length > 0)
            {
                EventLog.WriteEntry(Global.DISPLAY_NAME, "Error loading config: " + err, EventLogEntryType.Error);
                return false;
            }

            if (config.RemoteServer.Length == 0)
            {
                EventLog.WriteEntry(Global.DISPLAY_NAME, "Remote server not set in config", EventLogEntryType.Error);
                return false;
            }

            if (config.CertificateFileName.Length == 0)
            {
                EventLog.WriteEntry(Global.DISPLAY_NAME, "Certificate file name not set in config", EventLogEntryType.Error);
                return false;
            }

            if (System.IO.File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.CertificateFileName)) == false)
            {
                EventLog.WriteEntry(Global.DISPLAY_NAME, "Certificate does not exist in application directory", EventLogEntryType.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pemFile"></param>
        /// <returns></returns>
        private bool LoadX509Certificate()
        {
            try
            {
                var x509Cert = X509Certificate.CreateFromCertFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.CertificateFileName));
                ExtendedHttpClient.x509Cert = x509Cert;
                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Global.DISPLAY_NAME, "Error loading certificate: " + ex.Message, EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer.Enabled = false;
            SendDataToServer();
            this.timer.Enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SendDataToServer()
        {
            lock (locker)
            {
                // Retrieve the current set of data
                sendData = String.Join("###SML###", this.data);
                data.Clear();
            }

            if (sendData.Length == 0)
            {
                return;
            }

            Task.Factory.StartNew(() => { ehc.Send(remoteUrl, sendData); });
        }

        #region ExtendedHttpClient Message Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnEhc_Error(string message)
        {
            // Only log an ExtendedHttpClient error once
            if (this.sendError == false)
            {
                this.sendError = true;
                EventLog.WriteEntry(Global.DISPLAY_NAME, message, EventLogEntryType.Error);
            }
            
        }
        #endregion
    }
}
