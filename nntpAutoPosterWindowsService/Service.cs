using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using log4net;
using nntpAutoposter;
using Util.Configuration;

namespace nntpAutoPosterWindowsService
{
    public partial class Service : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(
          System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Watcher watcher;
        AutoPoster poster;
        IndexerNotifierBase notifier;
        IndexerVerifierBase verifier;
        DatabaseCleaner cleaner;

        public Service()
        {
            InitializeComponent();
            this.CanPauseAndContinue = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                var configuration = Settings.LoadSettings();

                watcher = new Watcher(configuration);
                watcher.Start();
                log.Info("FileSystemWatcher started");

                poster = new AutoPoster(configuration);
                poster.Start();
                log.Info("Autoposter started");

                notifier = IndexerNotifierBase.GetActiveNotifier(configuration);
                if (notifier != null)
                {
                    notifier.Start();
                    log.Info("Notifier started");
                }
                else
                {
                    log.Info("No notifier");
                }

                verifier = IndexerVerifierBase.GetActiveVerifier(configuration);
                if (verifier != null)
                {
                    verifier.Start();
                    log.Info("Verifier started");
                }
                else
                {
                    log.Info("No verifier");
                }

                cleaner = new DatabaseCleaner(configuration);
                cleaner.Start();
                log.Info("DB Cleaner started");
            }
            catch (Exception ex)
            {
                log.Fatal("Fatal exception when starting the autoposter.", ex);
                throw;
            }
        }

        protected override void OnPause()
        {
            try
            {
                cleaner.Stop(2000);
                log.Info("DB Cleaner stopped");

                watcher.Stop(2000);
                log.Info("FileSystemWatcher stopped");

                poster.Stop();  //This call will block until the current item is done posting.
                log.Info("Autoposter stopped");

                if (verifier != null)
                {
                    verifier.Stop(2000);
                    log.Info("Verifier stopped");
                }

                if (notifier != null)
                {
                    notifier.Stop(2000);
                    log.Info("Notifier stopped");
                }
            }
            catch (Exception ex)
            {
                log.Fatal("Fatal exception when stopping the autoposter.", ex);
                throw;
            }
            log.Info("Shuttong down the service after a clean stop (pause) request");
            Environment.Exit(0);
        }

        protected override void OnContinue()
        {
            OnStart(null);
        }
        
        protected override void OnStop()
        {
            try
            {
                cleaner.Stop(2000);
                log.Info("DB Cleaner stopped");

                watcher.Stop(2000);
                log.Info("FileSystemWatcher stopped");

                if (verifier != null)
                {
                    verifier.Stop(2000);
                    log.Info("Verifier stopped");
                }

                if (notifier != null)
                {
                    notifier.Stop(2000);
                    log.Info("Notifier stopped");
                }                

                poster.Stop(20000);
                log.Info("Autoposter stopped");
            }
              catch (Exception ex)
              {
                  log.Fatal("Fatal exception when stopping the autoposter.", ex);
                  throw;
              }
        }
    }
}
