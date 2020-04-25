using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idm_frontend_mock
{
    public class AppSettings
    {
        public string Instance { get; set; }
        public string ApiUrl { get; set; }
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string AuthClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CertificateName { get; set; }
        public string IDMServiceExePath { get; set; }
    }
}
