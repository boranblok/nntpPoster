using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace nntpAutoPosterWindowsService
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public Installer()
        {
            InitializeComponent();
        }

        private void RegisterServiceInstaller()
        {
            String serviceName = "NNTPAutoPoster";
            String serviceDisplayName = "NNTP Auto Poster";

            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.NetworkService;
            service = new ServiceInstaller();
            service.ServiceName = serviceName;
            service.DisplayName = serviceDisplayName;
            service.Description = "This service monitors a specific folder and uploads all files and folders placed there to usenet.";
            service.StartType = ServiceStartMode.Automatic;
            service.ServicesDependedOn = new String[] { };
            Installers.Add(process);
            Installers.Add(service);
        }

        public override void Install(IDictionary stateSaver)
        {
            RegisterServiceInstaller();
            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            RegisterServiceInstaller();
            base.Uninstall(savedState);
        }
    }
}
