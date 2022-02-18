using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace CoreUtils.Classes
{

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
            if (IsBlank(value))
            {
                return value;
            }

            int charsToTake = Math.Min(length, value.Length);
            return value.Substring(0, charsToTake);
        }
        public static string Right(string value, int length)
        {
            if (IsBlank(value))
            {
                return value;
            }

            int charsToTake = Math.Min(length, value.Length);
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


        public static Boolean TextMatchesPattern(string fileName, string pattern)
        {
            if (Operators.LikeString(fileName, pattern, CompareMethod.Text))
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

        //extension method to add desc attribute to enum item
        public class DisplayText : Attribute
        {
            public DisplayText(string text)
            {
                this.text = text;
            }

            public string text { get; set; }
        }

        public static Boolean IsValueOfFormat(string value, FormatType formatType)
        {
            if (formatType == FormatType.Any)
            {
                return true;
            }

            return true;
        }
        public static bool IsNumeric(string value)
        {
            var isNumeric = float.TryParse(value, out _);
            return isNumeric;
        }

        public static float ToNumber(string value)
        {
            var isNumeric = float.TryParse(value, out var number);
            return number;
        }  
        
        public static DateTime ToDateTime(string value)
        {
            var dateTime = DateTime.TryParse(value, out var number);
            return number;
        }
        
        public static string ToDateString (DateTime value)
        {
            var str =  value.ToShortDateString();
            return str;
        }
        public static string ToTimeString (DateTime value)
        {
            var str =  value.ToShortTimeString();
            return str;
        }
        public static string ToDateTimeString (DateTime value)
        {
            var str =  $"{ToDateString(value)} {ToTimeString(value)} ";
            return str;
        }

    }
}