using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace CoreUtils.Classes
{
    public class LogFields
    {
        public string LogTime { get; }
        public string FileId { get; }
        public string Task { get; }
        public string Status { get; }
        public string FileName { get; }
        public string OutcomeDetails { get; }

        public LogFields(string logTime, string fileId, string task, string status, string fileName, string outcomeDetails)
        {
            LogTime = logTime;
            FileId = fileId;
            Task = task;
            Status = status;
            FileName = fileName;
            OutcomeDetails = outcomeDetails;
        }

        public override string ToString()
        {
            return $"{LogTime}\t{FileId}\t{Task}\t{Status}\t{FileName}\t{OutcomeDetails}";
        }
    }

    public static class Utils
    {

        private static readonly Random Random = new Random();

        public static string DbQuote(string value)
        {
            return value.Replace("'", "''");
        }



        public static string CsvQuote(string value)
        {
            return Convert.ToChar(34) +
                   value.Replace($"{Convert.ToChar(34)}", $"{Convert.ToChar(34)}{Convert.ToChar(34)}") +
                   Convert.ToChar(34);
        }

        public static string Left(string value, int length)
        {
            if (IsBlank(value)) return value;

            var charsToTake = Math.Min(length, value.Length);
            return value.Substring(0, charsToTake);
        }

        public static string Right(string value, int length)
        {
            if (IsBlank(value)) return value;

            var charsToTake = Math.Min(length, value.Length);
            return value.Length <= length ? value : value.Substring(value.Length - charsToTake);
        }

        public static bool IsBlank(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;

            return false;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
          public static string RandomStringNumbers(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        //extension method to get desc of enum item
        public static string ToDescription(this Enum en)
        {
            var type = en.GetType();

            var memInfo = type.GetMember(en.ToString());

            if (memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(
                    typeof(DisplayText),
                    false);

                if (attrs.Length > 0)

                    return ((DisplayText)attrs[0]).text;
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


        public static bool TextMatchesPattern(string fileName, string pattern)
        {
            if (Operators.LikeString(fileName, pattern, CompareMethod.Text)) return true;

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
            var isNumeric = Int64.TryParse(value, out _);
            return isNumeric;
        }

        public static bool IsDouble(string value)
        {
            var isNumeric = float.TryParse(value, out _);
            return isNumeric;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var mail = new System.Net.Mail.MailAddress(email);
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
            var isNumeric = float.TryParse(value, out var number);
            return number;
        }

        public static bool IsIsoDate(string value, Boolean checkNotNull = true)
        {
            if (Utils.IsBlank(value))
            {
                if (checkNotNull)
                {
                    return false;
                }
                return true;
            }

            Boolean parsed = DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out var aDate);
            if (!parsed)
            {
                return false;
            }

            return true;
        }
        public static bool IsIsoDateTime(string value, Boolean checkNotNull = true)
        {
            if (Utils.IsBlank(value))
            {
                if (checkNotNull)
                {
                    return false;
                }
                return true;
            }

            Boolean parsed = DateTime.TryParseExact(value, "yyyyMMdd HHmmss", null, DateTimeStyles.None, out var aDate);
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
            DateTime? aDate = Utils.ToDateTime(value);

            aDate = aDate?.Date;
            return aDate;

        }
        public static DateTime? ToDateTime(string value)
        {
            if (Utils.IsBlank(value))
            {
                return null;
            }

            Boolean parsed = false;

            // ISODateTime
            parsed = DateTime.TryParseExact(value, "yyyyMMdd HH:mm:ss", null, DateTimeStyles.None, out var aDateTimeIso);
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
            parsed = DateTime.TryParseExact(value, "dd-MMM-yyyy hh:mm:ss tt", null, DateTimeStyles.None, out var aDateTimeUs);
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
            parsed = DateTime.TryParseExact(value, "dd-MMM-yy hh:mm:ss tt", null, DateTimeStyles.None, out var aDateTimeUs2);
            if (parsed)
            {
                return aDateTimeUs2;
            }

            // us long date time 2 yr
            parsed = DateTime.TryParseExact(value, "dd-MMM-yy", null, DateTimeStyles.None, out var aDateUs2);
            if (parsed)
            {
                return aDateUs2;
            }


            return null;
        }

        public static string ToDateString(DateTime? value)
        {
            if (value == null)
            {
                return "";
            }
            var str = value?.ToShortDateString();
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

        public static string GetExeBaseDir()
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                var exePath = processModule.FileName;
                var directoryPath = Path.GetDirectoryName(exePath);
                return directoryPath;
            }

            return "";
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
    }
}