using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace CoreUtils.Classes
{

    public class IncorrectFileFormatException : Exception
    {
        public IncorrectFileFormatException(string message) : base(message)
        {
        }
    }
    public class EmailBrokerStatementsException : Exception
    {
        public EmailBrokerStatementsException(string message) : base(message)
        {
        }
    }
    public class ImportException : Exception
    {
        public ImportException(string message) : base(message)
        {
        }
    }

    public class OpenExcelFileException : Exception
    {
        public OpenExcelFileException(string message) : base(message)
        {
        }
    }

    public class JobDetails
    {
        public string JobErrorDetails;
        public string JobHistory;
        public string JobId;
        public string JobMessage;
        public string JobName;
        public string JobResultDetails;
        public string JobState;

        public JobDetails()
        {
        }

        public JobDetails(string jobName, string jobId, string jobMessage, string jobState = "", string jobHistory = "",
            string jobResultDetails = "", string jobErrorDetails = "")
        {
            this.JobName = jobName;
            this.JobMessage = jobMessage;
            this.JobId = jobId;
            this.JobState = jobState;
            this.JobHistory = jobHistory;
            this.JobResultDetails = jobResultDetails;
            this.JobErrorDetails = jobErrorDetails;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class OperationResult
    {
        public string Code;
        public string Details;
        public string Error;
        public string Result;
        public int Success;

        public OperationResult(int success, string code, string result = "", string details = "", string error = "")
        {
            this.Success = success;
            this.Code = code;
            this.Error = error;
            this.Result = result;
            this.Details = details;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
            //return $"Success: {Success}\n<br>Code: {Code}\n<br>Result: {Result}\n<br>Details: {Details}\n<br>Error: {Error}";
        }
    }

    public class LogFields
    {
        public LogFields(string logTime, string fileId, string task, string status, string fileName,
            string outcomeDetails)
        {
            this.LogTime = logTime;
            this.FileId = fileId;
            this.Task = task;
            this.Status = status;
            this.FileName = fileName;
            this.OutcomeDetails = outcomeDetails;
        }

        public string LogTime { get; set; }
        public string FileId { get; set; }
        public string Task { get; set; }
        public string Status { get; set; }
        public string FileName { get; set; }
        public string OutcomeDetails { get; set; }

        public override string ToString()
        {
            return $"{this.LogTime}\t{this.FileId}\t{this.Task}\t{this.Status}\t{this.FileName}\t{this.OutcomeDetails}";
        }
    }

    public static class Utils
    {
        private static readonly Random Random = new Random();

        public static string FilePartsDelimiter = "--";

        public static int MaxFilenameLengthFtp = 60;

        public static string DbQuote(string value)
        {
            return value.Replace("'", "''");
        }

        // thjios deserializes doubly escaped json also!
        public static object DeserializeJson<T>(string value)
        {
            try
            {
                if (IsBlank(value))
                {
                    return null;
                }

                // try to unescape doubly quoted json
                var try1 = JsonConvert.DeserializeObject(value);
                if (try1 is string)
                {
                    value = (string)try1;
                }

                object deserializeObject = JsonConvert.DeserializeObject<T>(value);

                return deserializeObject;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string SerializeJson(object value)
        {
            try
            {
                if (value == null)
                {
                    return "";
                }

                var serializedvalue = JsonConvert.SerializeObject(value);

                return serializedvalue;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string CsvQuote(string value)
        {
            return Convert.ToChar(34) +
                   value.Replace($"{Convert.ToChar(34)}", $"{Convert.ToChar(34)}{Convert.ToChar(34)}") +
                   Convert.ToChar(34);
        }

        public static string Left(string value, int length)
        {
            if (IsBlank(value))
            {
                return value;
            }

            var charsToTake = Math.Min(length, value.Length);
            return value.Substring(0, charsToTake);
        }

        public static string Right(string value, int length)
        {
            if (IsBlank(value))
            {
                return value;
            }

            var charsToTake = Math.Min(length, value.Length);
            return value.Length <= length ? value : value.Substring(value.Length - charsToTake);
        }
        public static string Mid(string value, int start, int length)
        {
            if (IsBlank(value))
            {
                return value;
            }

            var charsToTake = Math.Min(start + length, value.Length);

            return value.Length <= start + length ? value : value.Substring(start - 1, value.Length - charsToTake - 1);
        }
        public static string FormatSsnWithDashes(string value)
        {
            value = value?.Trim();

            if (Utils.IsBlank(value))
            {
                return "";
            }

            value = FormatSsnNumbersOnly(value);

            value = value.PadLeft(9, '0');

            return $"{Left(value, 3)}-{Mid(value, 4, 2)}-{Right(value, 4)}";

        }
        public static string FormatSsnNumbersOnly(string value)
        {
            value = value?.Trim();

            if (Utils.IsBlank(value))
            {
                return "";
            }

            value = regexInteger.Replace(value, String.Empty);
            return value;

        }


        public static bool IsBlank(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return false;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Utils.Random.Next(s.Length)]).ToArray());
        }

        public static string RandomStringNumbers(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Utils.Random.Next(s.Length)]).ToArray());
        }

        //extension method to get desc of enum item
        public static string ToDescription(this Enum en)
        {
            var type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(
                    typeof(DisplayText),
                    false);

                if (attrs.Length > 0)

                {
                    return ((DisplayText)attrs[0]).text;
                }
            }

            return en.ToString();
        }

        //extension method to get number of enum item
        public static string ToNumberString(this Enum enVal)
        {
            return Convert.ToDecimal(enVal).ToString("0");
        }

        //extension method to get string from array
        public static string Join(this object[] arr, string joinWith)
        {
            return string.Join(joinWith, arr);
        }


        public static bool TextMatchesPattern(string text, string pattern)
        {
            if (Operators.LikeString(text, pattern, CompareMethod.Text))
            {
                return true;
            }

            return false;
        }

        //extension method to allow with
        public static T With<T>(this T item, Action<T> action)
        {
            action(item);
            return item;
        }


        public static bool IsInteger(string value)
        {
            value = value?.Trim();

            var isNumeric = long.TryParse(value, out _);
            return isNumeric;
        }

        public static bool IsDouble(string value)
        {
            value = value?.Trim();
            var isNumeric = float.TryParse(value, out _);
            return isNumeric;
        }

        public static float ToDouble(string value)
        {
            value = value?.Trim();
            float dblValue;
            var isNumeric = float.TryParse(value, out dblValue);
            if (isNumeric)
            {
                return dblValue;
            }

            return 0;
        }
        public static decimal ToDecimal(string? value)
        {
            value = value?.Trim();
            decimal dblValue;
            var isNumeric = decimal.TryParse(value, out dblValue);
            if (isNumeric)
            {
                return dblValue;
            }

            return 0;
        }

        public static decimal ToDecimal(decimal? value)
        {
            if (value != null)
            {
                return (decimal)value;
            }

            return 0;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                email = email?.Trim();
                var mail = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNumeric(string value)
        {
            return IsDouble(value);
        }

        public static float ToNumber(string value)
        {
            value = value?.Trim();
            var isNumeric = float.TryParse(value, out var number);
            return number;
        }
        public static int ToInt(string value)
        {
            value = value?.Trim();
            var parsed = int.TryParse(value, out var number);
            if (!parsed)
            {
                return 0;
            }

            return number;
        }

        public static bool IsIsoDate(string value, bool checkNotNull = true)
        {
            if (IsBlank(value))
            {
                if (checkNotNull)
                {
                    return false;
                }

                return true;
            }

            var parsed = DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out var aDate);
            if (!parsed)
            {
                return false;
            }

            return true;
        }
        public static bool IsDate(string value)
        {
            if (IsBlank(value))
            {
                return false;
            }

            var parsed = DateTime.TryParse(value, out var dt);
            if (!parsed)
            {
                return false;
            }

            return true;
        }

        public static bool IsIsoDateTime(string value, bool checkNotNull = true)
        {
            value = value?.Trim();
            if (IsBlank(value))
            {
                if (checkNotNull)
                {
                    return false;
                }

                return true;
            }

            var parsed = DateTime.TryParseExact(value, "yyyyMMdd HHmmss", null, DateTimeStyles.None, out var aDate);
            if (!parsed)
            {
                return false;
            }

            if (aDate == DateTime.MinValue && checkNotNull)
            {
                return false;
            }

            return true;
        }

        public static DateTime? ToDate(string value)
        {
            DateTime? aDate = ToDateTime(value);

            aDate = aDate?.Date;
            return aDate;
        }

        public static DateTime? ToDateTime(string value)
        {
            value = value?.Trim();
            if (IsBlank(value))
            {
                return null;
            }

            var parsed = false;

            // ISODateTime
            parsed = DateTime.TryParseExact(value, "yyyyMMdd HH:mm:ss", null, DateTimeStyles.None,
                out var aDateTimeIso);
            if (parsed)
            {
                return aDateTimeIso;
            }

            // ISODate
            parsed = DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out var aDateIso);
            if (parsed)
            {
                return aDateIso;
            }

            // US Long date Time
            parsed = DateTime.TryParseExact(value, "dd-MMM-yyyy hh:mm:ss tt", null, DateTimeStyles.None,
                out var aDateTimeUs);
            if (parsed)
            {
                return aDateTimeUs;
            }

            // US Long date 
            parsed = DateTime.TryParseExact(value, "dd-MMM-yyyy", null, DateTimeStyles.None, out var aDateUs);
            if (parsed)
            {
                return aDateUs;
            }

            // us long date time 2 yr
            parsed = DateTime.TryParseExact(value, "dd-MMM-yy hh:mm:ss tt", null, DateTimeStyles.None,
                out var aDateTimeUs2);
            if (parsed)
            {
                return aDateTimeUs2;
            }

            // us short date time 
            parsed = DateTime.TryParseExact(value, "d-M-yyyy hh:mm:ss tt", null, DateTimeStyles.None, out var aDateUs2);
            if (parsed)
            {
                return aDateUs2;
            }

            // try default Parse
            return DateTime.Parse(value);


        }

        public static string ToDateString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            var str = ToIsoDateString(value);
            return str;
        }
        public static string ToUsDateString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            if (value == null)
            {
                return "";
            }

            var str = value?.ToString("MM/dd/yyyy");
            return str;
        }

        public static string ToIsoDateString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            var str = value?.ToString("yyyyMMdd");
            return str;
        }


        public static string ToIsoDateString(Object value)
        {
            if (value == null)
            {
                return "";
            }

            if (Utils.IsDate(value.ToString()))
            {
                var dt = (DateTime)value;
                return ToIsoDateString(dt);
            }

            return "";

        }

        public static string ToIsoDateTimeString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            var str = value?.ToString("yyyyMMdd HH:mm:ss");
            return str;
        }

        public static string ToTimeString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            var str = value?.ToShortTimeString();
            return str;
        }

        public static string ToDateTimeString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }

            var str = $"{ToDateString(value)} {ToTimeString(value)} ";
            return str;
        }


        public static string GetConnString(string connStringName)
        {
            var entityConnectionString = ConfigurationManager.ConnectionStrings[connStringName].ConnectionString;
            return entityConnectionString;
        }

        public static string GetAppSetting(string settingName)
        {
            dynamic section = ConfigurationManager.GetSection("AppSettings");
            var keys = section.Keys;
            foreach (var key in keys)
            {
                if (key == settingName)
                {
                    return section[key];
                }
            }

            return "";
        }

        public static string GetProviderConnString(string connStringName)
        {
            var entityConnectionString = GetConnString(connStringName);
            if (entityConnectionString.ToLower().IndexOf(@"provider connection string=", StringComparison.Ordinal) > 0)
            {
                return new EntityConnectionStringBuilder(entityConnectionString).ProviderConnectionString;
            }

            return entityConnectionString;
        }

        public static bool IsTestFile(string srcFilePath)
        {
            var fileInfo = new FileInfo(srcFilePath);

            if ((fileInfo.Name?.ToLower()).IndexOf("test", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }


        public static string GetUniqueIdFromFileName(string fileName)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var indexOfSep = fileNameWithoutExt.IndexOf($"{Utils.FilePartsDelimiter}", StringComparison.Ordinal);
            if (indexOfSep > 0)
            {
                string uniqueId = $"{fileNameWithoutExt.Substring(indexOfSep + Utils.FilePartsDelimiter.Length).Trim()}";
                return uniqueId;
            }

            return "";
        }

        public static HeaderType GetHeaderTypeFromFileName(string fileName)
        {
            var fileNameParts = fileName?.Split(new[] { Utils.FilePartsDelimiter }, StringSplitOptions.None);
            if (fileNameParts != null && fileNameParts.Length < 3)
            {
                return HeaderType.NotApplicable;
            }

            if (fileNameParts != null)
            {
                var strHeaderType = fileNameParts[1];
                if (strHeaderType == HeaderType.NotApplicable.ToNumberString())
                {
                    return HeaderType.NotApplicable;
                }

                if (strHeaderType == HeaderType.New.ToNumberString())
                {
                    return HeaderType.New;
                }

                if (strHeaderType == HeaderType.Old.ToNumberString())
                {
                    return HeaderType.Old;
                }

                if (strHeaderType == HeaderType.NoChange.ToNumberString())
                {
                    return HeaderType.NoChange;
                }

                if (strHeaderType == HeaderType.Own.ToNumberString())
                {
                    return HeaderType.Own;
                }

                return HeaderType.NotApplicable;
            }

            return HeaderType.NotApplicable;
        }

        public static string StripUniqueIdAndHeaderTypeFromFileName(string fileName)
        {
            var fileNameParts = fileName?.Split(new[] { Utils.FilePartsDelimiter }, StringSplitOptions.None);

            // return the last part of the fileName
            if (fileNameParts != null)
            {
                return fileNameParts[0] ?? "";
            }

            return "";

        }

        public static string AddUniqueIdAndHeaderTypeToFileName(string fileName,
            HeaderType headerType = HeaderType.NotApplicable)
        {
            fileName = StripUniqueIdAndHeaderTypeFromFileName(fileName);

            var fileId = RandomString(3);
            fileName =
                $"{fileId}{Utils.FilePartsDelimiter}{headerType.ToNumberString()}{Utils.FilePartsDelimiter}{fileName}";
            //
            fileName = fileName.Trim();
            //
            return fileName;
        }

        public static string AddUniqueIdToFileName(string fileName)
        {
            string filenameNoExt = Path.GetFileNameWithoutExtension(fileName);
            filenameNoExt = StripUniqueIdAndHeaderTypeFromFileName(filenameNoExt);

            // sumeet: use timestamp instead of random numbers
            // var fileId = RandomStringNumbers(3);
            var fileId = DateTime.Now.ToString("HHmmssfff");
            fileName = $"{filenameNoExt}{Utils.FilePartsDelimiter}{fileId}{Path.GetExtension(fileName)}";
            //
            fileName = fileName.Trim();
            //
            return fileName;
        }


        //extension method to add desc attribute to enum item
        public class DisplayText : Attribute
        {
            public DisplayText(string text)
            {
                this.text = text;
            }

            public string text { get; set; }
        }


        public static readonly Regex regexInteger = new Regex("[^0-9]");
        public static readonly Regex regexDate = new Regex(@"[^a-zA-Z0-9\s:\-\//]");
        public static readonly Regex regexAlphaNumeric = new Regex(@"[^a-zA-Z0-9\s]");
        public static readonly Regex regexAlphaOnly = new Regex(@"[^a-zA-Z]");
        public static readonly Regex regexAlphaAndDashes = new Regex(@"[^a-zA-Z\-]");
        public static readonly Regex regexAlphaNumericAndDashes = new Regex(@"[^a-zA-Z0-9\-]");
        public static readonly Regex regexNumericAndDashes = new Regex(@"[^0-9\-]");
        public static readonly Regex regexDouble = new Regex("[^0-9.]");

    }
}