using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace Xeon.IO
{
    public delegate string EscapeFunction(string csv, Dictionary<string, string> escapedData);
    public static class CsvUtility
    {
        public static readonly Regex StringRegex = new Regex(@"("".*?"")");
        public static readonly Regex ListRegex = new Regex(@"(\[.*?\])");
        public static readonly Regex ObjectRegex = new Regex(@"({.*?})");
        public static readonly Regex VectorRegex = new Regex(@"(\(.*?\))");

        private static List<EscapeFunction> EscapeFunctions = new()
        {
            EscapeString,
            EscapeVector,
            EscapeObject,
            EscapeList,
        };

        public static void ResetEscapeFunctions() => EscapeFunctions = new()
        {
            EscapeString,
            EscapeVector,
            EscapeObject,
            EscapeList,
        };

        public static void AddEscapeFunctions(string name, Regex regex)
        {
            EscapeFunctions.Add((csv, escapedData) =>
                Escape(csv, regex, $"<escaped {name}>{{0}}</escaped {name}>", escapedData));
        }

        public static void InsertEscapeFunctions(int index, string name, Regex regex)
        {
            EscapeFunctions.Insert(index, (csv, escapedData) =>
                Escape(csv, regex, $"<escaped {name}>{{0}}</escaped {name}>", escapedData));
        }

        public static string Escape(string csv, Dictionary<string, string> escapedData)
        {
            var result = csv;
            foreach (var function in EscapeFunctions)
                result = function(result, escapedData);
            return result;
        }

        public static string EscapeVectorRegex(string csv, Dictionary<string, string> escapedData)
            => EscapeByRegex(VectorRegex, csv, escapedData, "vector");
        public static string EscapeListRegex(string csv, Dictionary<string, string> escapedData)
            => EscapeByRegex(ListRegex, csv, escapedData, "list");
        public static string EscapeObjectRegex(string csv, Dictionary<string, string> escapedData)
            => EscapeByRegex(ObjectRegex, csv, escapedData, "object");
        public static string EscapeStringRegex(string csv, Dictionary<string, string> escapedData)
            => EscapeByRegex(StringRegex, csv, escapedData, "string");

        private static string EscapeByRegex(Regex regex, string csv, Dictionary<string, string> escapedData, string name)
        {
            var result = csv;
            var replaceTexts = new List<string>();
            while(regex.IsMatch(result))
            {
                foreach (Match match in regex.Matches(result))
                {
                    if (match.Groups.Count < 2) continue;
                    var value = match.Groups[1].Value;
                    var escapeIndex = replaceTexts.IndexOf(value);
                    if (escapeIndex < 0)
                    {
                        escapeIndex = escapedData.Count;
                        replaceTexts.Add(value);
                    }
                    var replaceText = $"<escaped {name}>{escapeIndex}</escaped {name}>";
                    result = result.Replace(value, replaceText);
                    if (!escapedData.ContainsKey(replaceText))
                        escapedData.Add(replaceText, value);
                }
            }
            return result;
        }

        public static string EscapeVector(string csv, Dictionary<string, string> escapedData)
            => EscapeBrackets(csv, "vector", '(', ')', escapedData);
        public static string EscapeList(string csv, Dictionary<string, string> escapedData)
            => EscapeBrackets(csv, "list", '[', ']', escapedData);
        public static string EscapeObject(string csv, Dictionary<string, string> escapedData)
            => EscapeBrackets(csv, "object", '{', '}', escapedData);

        public static string EscapeString(string csv, Dictionary<string, string> escapedData)
            => EscapeBrackets(csv, "string", '"', '"', escapedData);

        private static string EscapeBrackets(string csv, string name, char start, char end, Dictionary<string, string> escapedData)
        {
            var result = csv;

            var replaceTexts = new List<string>();
            var startIndex = -1;
            for (var index = 0; index < csv.Length; index++)
            {
                if (startIndex < 0)
                {
                    if (csv[index] != start) continue;
                    startIndex = index;
                    continue;
                }
                if (csv[index] != end) continue;
                var endIndex = index + 1;
                var target = csv[startIndex..endIndex];
                var escapeIndex = replaceTexts.IndexOf(target);
                if (escapeIndex < 0)
                {
                    escapeIndex = escapedData.Count;
                    replaceTexts.Add(target);
                }
                var replaceText = $"<escaped {name}>{escapeIndex}</escaped {name}>";
                result = result.Replace(target, replaceText);
                if (!escapedData.ContainsKey(replaceText))
                    escapedData.Add(replaceText, target);
                startIndex = -1;
            }
            return result;
        }

        private static string Escape(string csv, Regex regex, string replaceFormat, Dictionary<string, string> escapedData)
        {
            var result = csv;
            var matchData = new Dictionary<string, string>();
            foreach (var (match, index) in regex.Matches(csv).Select((match, index) => (match, index)))
            {
                var matchText = match.Groups[1].Value;
                if (matchData.ContainsKey(matchText)) continue;
                matchData.Add(matchText, string.Format(replaceFormat, index));
            }

            foreach (var (before, after) in matchData)
            {
                result = result.Replace(before, after);
                escapedData.Add(after, before);
            }
            return result;
        }
    }
}
