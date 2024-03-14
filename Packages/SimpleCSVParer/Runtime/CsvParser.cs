using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Xeon.IO
{

    public class CsvParser
    {
        public static List<Dictionary<string, string>> Parse(string path, string separator = "\t")
        {
            var encoding = EncodeHelper.GetJpEncoding(path);
            var result = new List<Dictionary<string, string>>();
            using (var streamReader = new StreamReader(path, encoding))
            {
                var headers = streamReader.ReadLine().Trim().Split(separator);
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var columns = line.Split(separator);
                    var row = new Dictionary<string, string>();
                    foreach ((var key, var index) in headers.Select((key, index) => (key, index)))
                        row[key] = columns[index];
                    result.Add(row);
                }
            }
            return result;
        }

        public static List<T> Parse<T>(string path, string separator = "\t") where T : CsvData, new()
        {
            var parsed = Parse(path, separator);
            var type = typeof(T);
            (var attributes, var members) = GetProperties<T>();
            var result = new List<T>();
            foreach (var row in parsed)
            {
                var instance = new T();
                foreach (var pair in row)
                {
                    if (!attributes.ContainsKey(pair.Key) || !members.ContainsKey(pair.Key)) continue;
                    var member = members[pair.Key];
                    try
                    {
                        if (member.MemberType == MemberTypes.Property)
                            type.GetProperty(member.Name).SetValue(instance, pair.Value);
                        else if (member.MemberType == MemberTypes.Field)
                            SetValue(type, member.Name, instance, pair.Value);
                        else
                            Debug.LogError($"{pair.Key} is not property or field");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                instance.Initialize();
                result.Add(instance);
            }
            return result;
        }

        private static void SetValue<T>(Type type, string memberName, T instance, string value)
        {
            var fieldInfo = type.GetField(memberName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo.FieldType == typeof(int) && int.TryParse(value, out var intValue))
                fieldInfo.SetValue(instance, intValue);
            else if (fieldInfo.FieldType == typeof(float) && float.TryParse(value, out var floatValue))
                fieldInfo.SetValue(instance, floatValue);
            else if (fieldInfo.FieldType == typeof(bool) && bool.TryParse(value, out var boolValue))
                fieldInfo.SetValue(instance, boolValue);
            else if (fieldInfo.FieldType == typeof(string))
                fieldInfo.SetValue(instance, value);
            else if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                SetArrayValue(fieldInfo, instance, value);
            else if (fieldInfo.FieldType.IsEnum)
                fieldInfo.SetValue(instance, Enum.Parse(fieldInfo.FieldType, value));
            else if (fieldInfo.FieldType == typeof(Vector2))
            {
                var splited = value.Split(',').Select(text => float.Parse(text)).ToList();
                fieldInfo.SetValue(instance, new Vector2(splited[0], splited[1]));
            }
            else if (fieldInfo.FieldType == typeof(Vector2Int))
            {
                var splited = value.Split(',').Select(text => int.Parse(text)).ToList();
                fieldInfo.SetValue(instance, new Vector2Int(splited[0], splited[1]));
            }
            else if (fieldInfo.FieldType == typeof(Vector3))
            {
                var splited = value.Split(',').Select(text => float.Parse(text)).ToList();
                fieldInfo.SetValue(instance, new Vector3(splited[0], splited[1], splited[2]));
            }
            else if (fieldInfo.FieldType == typeof(Vector3Int))
            {
                var splited = value.Split(',').Select(text => int.Parse(text)).ToList();
                fieldInfo.SetValue(instance, new Vector3Int(splited[0], splited[1], splited[2]));
            }
            else
                throw new Exception($"{fieldInfo.FieldType} is not supported");
        }

        private static void SetArrayValue<T>(FieldInfo fieldInfo, T instance, string value)
        {
            if (fieldInfo.FieldType.GenericTypeArguments.Length > 1)
                throw new InvalidCastException("Multi generic arguments type is not support");
            var type = fieldInfo.FieldType.GenericTypeArguments[0];
            var splited = value.Split(",").Where(splited => !string.IsNullOrEmpty(splited));
            if (type == typeof(int))
                fieldInfo.SetValue(instance, splited.Select(data => int.Parse(data)).ToList());
            else if (type == typeof(float))
                fieldInfo.SetValue(instance, splited.Select(data => float.Parse(data)).ToList());
            else if (type == typeof(bool))
                fieldInfo.SetValue(instance, splited.Select(data => bool.Parse(data)).ToList());
            else if (type == typeof(string))
                fieldInfo.SetValue(instance, splited.ToList());
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
                        value = type.GetField(member.Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(row);
                    else
                        continue;
                    values.Add(ToString(value));
                }
                builder.AppendLine(string.Join(separator, values));
            }
            return builder.ToString();
        }

        private static string ToString(object value)
        {
            if (value is Vector2 vector2)
                return $"{vector2.x},{vector2.y}";
            if (value is Vector3 vector3)
                return $"{vector3.x},{vector3.y},{vector3.z}";
            if (value is Vector2Int vector2Int)
                return $"{vector2Int.x},{vector2Int.y}";
            if (value is Vector3Int vector3Int)
                return $"{vector3Int.x},{vector3Int.y},{vector3Int.z}";
            if (value is not string && value is IEnumerable enumerable)
            {
                var array = new List<object>();
                foreach (var data in enumerable)
                    array.Add(data);
                return string.Join(",", array.ToArray());
            }
            return value.ToString();
        }

        private static (Dictionary<string, CsvColumn> attributes, Dictionary<string, MemberInfo> members) GetProperties<T>()
        {
            var type = typeof(T);
            var attributes = new Dictionary<string, CsvColumn>();
            var members = new Dictionary<string, MemberInfo>();
            foreach (var member in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(field => field as MemberInfo).Concat(type.GetProperties()))
            {
                var csvColumn = member.GetCustomAttribute<CsvColumn>();
                if (csvColumn == null) continue;
                attributes.Add(csvColumn.Name, csvColumn);
                members.Add(csvColumn.Name, member);
            }
            return (attributes, members);
        }
    }
}
