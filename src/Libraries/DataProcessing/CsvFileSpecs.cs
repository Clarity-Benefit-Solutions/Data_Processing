using CsvHelper.Configuration.Attributes;

// ReSharper disable InconsistentNaming

namespace DataProcessing
{
    public class CsvFileSpecs
    {
        private string _bencode;
        private string _clientStartDate;
        private string _crm;
        private string _crmEmail;
        private string _empServices;
        private string _primaryContactEmail;
        private string _primaryContactName;

        [Index(0)]
        public string BENCODE
        {
            get { return this._bencode?.Replace("\"", ""); }
            set { this._bencode = value; }
        }

        [Index(6)]
        public string client_start_date
        {
            get { return this._clientStartDate?.Replace("\"", ""); }
            set { this._clientStartDate = value; }
        }

        [Index(1)]
        public string CRM
        {
            get { return this._crm?.Replace("\"", ""); }
            set { this._crm = value; }
        }

        [Index(2)]
        public string CRM_email
        {
            get { return this._crmEmail?.Replace("\"", ""); }
            set { this._crmEmail = value; }
        }

        [Index(3)]
        public string emp_services
        {
            get { return this._empServices?.Replace("\"", ""); }
            set { this._empServices = value; }
        }

        [Index(5)]
        public string Primary_contact_email
        {
            get { return this._primaryContactEmail?.Replace("\"", ""); }
            set { this._primaryContactEmail = value; }
        }

        [Index(4)]
        public string Primary_contact_name
        {
            get { return this._primaryContactName?.Replace("\"", ""); }
            set { this._primaryContactName = value; }
        }

        //
        public string HeaderRow()
        {
            return @"BENCODE,CRM,CRM_email,emp_services,Primary_contact_name,Primary_contact_email,client_start_date";
        }
    }
}