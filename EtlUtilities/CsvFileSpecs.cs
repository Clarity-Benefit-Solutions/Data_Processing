using CsvHelper.Configuration.Attributes;

// ReSharper disable InconsistentNaming

namespace EtlUtilities
{
    public class CsvFileSpecs
    {
        private string _bencode;
        private string _crm;
        private string _crmEmail;
        private string _empServices;
        private string _primaryContactName;
        private string _primaryContactEmail;
        private string _clientStartDate;

        [Index(0)]
        public string BENCODE
        {
            get { return _bencode?.Replace("\"", ""); }
            set { _bencode = value; }
        }

        [Index(1)]
        public string CRM
        {
            get { return _crm?.Replace("\"", ""); }
            set { _crm = value; }
        }

        [Index(2)]
        public string CRM_email
        {
            get { return _crmEmail?.Replace("\"", ""); }
            set { _crmEmail = value; }
        }

        [Index(3)]
        public string emp_services
        {
            get { return _empServices?.Replace("\"", ""); }
            set { _empServices = value; }
        }

        [Index(4)]
        public string Primary_contact_name
        {
            get { return _primaryContactName?.Replace("\"", ""); }
            set { _primaryContactName = value; }
        }

        [Index(5)]
        public string Primary_contact_email
        {
            get { return _primaryContactEmail?.Replace("\"", ""); }
            set { _primaryContactEmail = value; }
        }

        [Index(6)]
        public string client_start_date
        {
            get { return _clientStartDate?.Replace("\"", ""); }
            set { _clientStartDate = value; }
        }

        //
        public string HeaderRow()
        {
            return @"BENCODE,CRM,CRM_email,emp_services,Primary_contact_name,Primary_contact_email,client_start_date";
        }
    }

}