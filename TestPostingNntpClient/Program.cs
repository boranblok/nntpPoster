using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostingNntpClient;

namespace TestNntpPostingClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //var connectionInfo = new NewsHostConnectionInfo();
            //connectionInfo.Address = ConfigurationManager.AppSettings["NewsGroupAddress"];
            //connectionInfo.Port = Int32.Parse(ConfigurationManager.AppSettings["NewsGroupPort"]);
            //connectionInfo.UseSsl = Boolean.Parse(ConfigurationManager.AppSettings["NewsGroupUseSsl"]);
            //connectionInfo.Username = ConfigurationManager.AppSettings["NewsGroupUsername"];
            //connectionInfo.Password = ConfigurationManager.AppSettings["NewsGroupPassword"];

            //using (var client = new SimpleNntpPostingClient(connectionInfo))
            //{
            //    client.Connect();
            //}
        }
    }
}
