using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Xeon.IO
{
    public class CsvParser
    {
        private const BindingFlags ProperyFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly Regex StringRegex = new Regex("(\".+\")");
        private static readonly Regex VectorRegex = new Regex(@"(\([\d\.\,\-]+\))\,");
        private static readonly Regex ListRegex = new Regex(@"\[(.+)\]");
        private static readonly Regex ObjectRegex = new Regex(@"{(.+)}");
        private static Dictionary<Regex, string> escapeRegexs = new()
        {
            { VectorRegex , "vector" },
            { ListRegex , "list" },
            { ObjectRegex, "object" },
        };

        public static void AddEscapeRegex(string name, Regex regex)
        {
            escapeRegexs.Remove(regex);
            escapeRegexs.Add(regex, name);
        }

        public static void RemoveEscapeRegex(Regex regex) => escapeRegexs.Remove(regex);

        public static void ResetEscapeRegex()
        {
            escapeRegexs.Clear(); ;
            AddEscapeRegex("vector", VectorRegex);
            AddEscapeRegex("list", ListRegex);
        }

        private string csv = null;
        private string separator = null;
        private Dictionary<string, string> escapedData = new();

        private CsvParser(string csv, string separator)
        {
            this.csv = csv.Replace("\r\n", "\n").Replace("\r", "\n");
            this.separator = separator;
            EscapeString();
            foreach (var (regex, name) in escapeRegexs)
                Escape(regex, $"<escaped {name}>{{0}}</escaped {name}>");
        }

        public string GetResult() => csv;
        public Dictionary<string, string> GetEscapedData() => escapedData;

        private void Escape(Regex regex, string replaceFormat)
        {
            var matchData = new Dictionary<string, string>();
            foreach (var (match, index) in regex.Matches(csv).Select((match, index) => (match, index)))
            {
                var matchText = match.Groups[1].Value;
                if (escapedData.ContainsKey(matchText) || matchData.ContainsKey(matchText)) continue;
                matchData.Add(matchText, string.Format(replaceFormat, index));
            }
            foreach (var (before, after) in matchData)
            {
                csv = csv.Replace(before, after);
                escapedData.Add(after, before);
            }
        }

        private void EscapeString()
        {
            var replaceTexts = new List<string>();
            var startIndex = -1;
            for (var index = 0; index < csv.Length; index++)
            {
                if (startIndex < 0)
                {
                    if (csv[index] != '"') continue;
                    startIndex = index;
                    continue;
                }
                if (csv[index] != '"') continue;
                var endIndex = index + 1;
                var target = csv[startIndex..endIndex];
                var escapeIndex = replaceTexts.IndexOf(target);
                if (escapeIndex < 0)
                {
                    escapeIndex = escapedData.Count;
                    replaceTexts.Add(target);
                }
                var replaceText = $"<escaped string>{escapeIndex}</escaped string>";
                csv = csv.Replace(target, replaceText);
                if (!escapedData.ContainsKey(replaceText))
                    escapedData.Add(replaceText, target);
                startIndex = -1;
            }
        }

        public static List<T> Parse<T>(string csv, string separator = "\t") where T : CsvData, new()
        {
            return new CsvParser(csv, separator).Parse<T>();
        }


        public static List<T> ParseFile<T>(string path, string separator = "\t") where T : CsvData, new()
        {
            var encoding = EncodeHelper.GetJpEncoding(path);
            using (var reader = new StreamReader(path, encoding))
            {
                var parser = new CsvParser(reader.ReadToEnd(), separator);
                return parser.Parse<T>();
            }
        }

        public List<T> Parse<T>() where T : CsvData, new()
        {
            var type = typeof(T);
            var (attributes, members) = GetProperties<T>();
            var result = new List<T>();
            var headers = new List<string>();
            bool isFirst = true;
            foreach (var line in csv.Split("\n").Select(line => line.Trim()))
            {
                if (string.IsNullOrEmpty(line)) continue;
                var columns = line.Split(separator);
                if (isFirst)
                {
                    headers = columns.ToList();
                    isFirst = false;
                    continue;
                }
                try
                {
                    var parsed = headers.Select((key, index) => (key, columns[index])).ToDictionary(pair => pair.key, pair => pair.Item2);
                    var instance = CreateInstance<T>(attributes, members, type, parsed);
                    result.Add(instance);
                }catch(Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError(string.Join(",", headers));
                    Debug.LogError(string.Join(",", columns));
                    foreach (var (key, data) in escapedData)
                        Debug.LogError($"{key}:{data}");
                }
            }
            return result;
        }

        private static (Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members) GetProperties<T>()
        {
            var type = typeof(T);
            var attributes = new Dictionary<string, CsvColumn>();
            var members = new Dictionary<string, MemberInfo>();
            var fields = type.GetFields(ProperyFlags).Select(field => field as MemberInfo);
            foreach (var member in fields.Concat(type.GetProperties()))
            {
                var csvColumn = member.GetCustomAttribute<CsvColumn>();
                if (csvColumn == null) continue;
                attributes.Add(csvColumn.Name, csvColumn);
                members.Add(csvColumn.Name, member);
            }
            return (attributes, members);
        }

        private T CreateInstance<T>(Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members,
            Type type, Dictionary<string, string> row)
            where T : CsvData, new()
        {
            var instance = new T();
            foreach (var (key, value) in row)
            {
                var text = value;
                if (!attributes.ContainsKey(key) || !members.ContainsKey(key)) continue;
                var member = members[key];
                try
                {
                    if (member.MemberType == MemberTypes.Property)
                    {
                        text = RestoreEscape(text);
                        type.GetProperty(member.Name).SetValue(instance, text);
                    }
                    else if (member.MemberType == MemberTypes.Field)
                        SetValue(type, member.Name, instance, text);
                    else
                        Debug.LogError($"{key} is not property or field");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            instance.Initialize();
            return instance;
        }

        private string RestoreEscape(string text)
        {
            while(true)
            {
                var isReplaced = false;
                foreach (var (escaped, origin) in escapedData)
                {
                    if (text.Contains(escaped))
                    {
                        text = text.Replace(escaped, origin);
                        isReplaced = true;
                    }
                }
                if (!isReplaced)
                    return text;
            }
        }

        private string Restore(string text, string escapeTarget)
        {
            while(true)
            {
                var isReplaced = false;
                foreach (var (escaped, origin) in escapedData)
                {
                    if (!escaped.Contains(escapeTarget)) continue;
                    if (text.Contains(escaped))
                    {
                        text = text.Replace(escaped, origin);
                        isReplaced = true;
                    }
                }
                if (!isReplaced)
                    return text;
            }
        }

        private void SetValue<T>(Type type, string memberName, T instance, string value)
        {
            var fieldInfo = type.GetField(memberName, ProperyFlags);
            value = Restore(value, "string");
            value = Restore(value, "object");
            value = Restore(value, "list");
            if (fieldInfo.FieldType.GetInterface(nameof(ICsvSupport)) != null)
            {
                var data = (ICsvSupport)Activator.CreateInstance(fieldInfo.FieldType);
                data.FromCsv(Restore(value, "string"));
                fieldInfo.SetValue(instance, data);
            }
            else if (fieldInfo.FieldType == typeof(int) && int.TryParse(value, out var intValue))
                fieldInfo.SetValue(instance, intValue);
            else if (fieldInfo.FieldType == typeof(float) && float.TryParse(value, out var floatValue))
                fieldInfo.SetValue(instance, floatValue);
            else if (fieldInfo.FieldType == typeof(bool) && bool.TryParse(value, out var boolValue))
                fieldInfo.SetValue(instance, boolValue);
            else if (fieldInfo.FieldType == typeof(string))
                fieldInfo.SetValue(instance, value.FromCsv());
            else if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                SetArrayValue(fieldInfo, instance, value);
            else if (fieldInfo.FieldType.IsEnum)
                fieldInfo.SetValue(instance, Enum.Parse(fieldInfo.FieldType, value));
            else if (fieldInfo.FieldType == typeof(Vector2))
                fieldInfo.SetValue(instance, value.ToVector2());
            else if (fieldInfo.FieldType == typeof(Vector2Int))
                fieldInfo.SetValue(instance, value.ToVector2Int());
            else if (fieldInfo.FieldType == typeof(Vector3))
                fieldInfo.SetValue(instance, value.ToVector3());
            else if (fieldInfo.FieldType == typeof(Vector3Int))
                fieldInfo.SetValue(instance, value.ToVector3Int());
            else if (fieldInfo.FieldType == typeof(Vector4))
                fieldInfo.SetValue(instance, value.ToVector4());
            else
                throw new Exception($"{fieldInfo.FieldType} is not supported");
        }

        private void SetArrayValue<T>(FieldInfo fieldInfo, T instance, string value)
        {
            value = value.Trim('[', ']');
            if (fieldInfo.FieldType.GenericTypeArguments.Length > 1)
                throw new InvalidCastException("Multi generic arguments type is not support");
            var type = fieldInfo.FieldType.GenericTypeArguments[0];
            var splited = value.Split(",").Where(splited => !string.IsNullOrEmpty(splited)).ToList();
            for (var index = 0; index < splited.Count; index++)
                splited[index] = Restore(splited[index], "string");
            if (type == typeof(int))
                fieldInfo.SetValue(instance, splited.Select(data => int.Parse(data)).ToList());
            else if (type == typeof(float))
                fieldInfo.SetValue(instance, splited.Select(data => float.Parse(data)).ToList());
            else if (type == typeof(bool))
                fieldInfo.SetValue(instance, splited.Select(data => bool.Parse(data)).ToList());
            else if (type == typeof(string))
                fieldInfo.SetValue(instance, splited.Select(data => data.FromCsv()).ToList());
            else if (type.IsEnum)
                fieldInfo.SetValue(instance, splited.Select(data => Enum.Parse(type, data)).ToList());
            else
                throw new InvalidCastException("This type's List is not support");
        }

        public static string ToCSV<T>(List<T> data, string separator = "\t")
        {
            var builder = new StringBuilder();
            (var attributes, var members) = GetProperties<T>();
            builder.AppendLine(string.Join(separator, attributes.Keys));
            var type = typeof(T);
            foreach (var row in data)
            {
                var values = new List<string>();
                foreach ((_, var member) in members)
                {
                    object value = null;
                    if (member.MemberType == MemberTypes.Property)
                        value = type.GetProperty(member.Name).GetValue(row);
                    else if (member.MemberType == MemberTypes.Field)
                        value = type.GetField(member.Name, ProperyFlags).GetValue(row);
                    else
                        continue;
                    values.Add(CsvSupport.ToString(value));
                }
                builder.AppendLine(string.Join(separator, values));
            }
            return builder.ToString();
        }
    }
}