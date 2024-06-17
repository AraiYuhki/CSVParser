using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.IO
{
    public class InvalidFormatException : Exception
    {
        public override string Message => "フォーマットが正しくありません";
    }
    public static class CsvSupport
    {
        public static string ToString(object value, string separator = ",")
        {
            if (value is string text)
                return text.ToCsv();
            if (value is Vector2 vector2)
                return vector2.ToCsv(separator);
            if (value is Vector3 vector3)
                return vector3.ToCsv(separator);
            if (value is Vector2Int vector2Int)
                return vector2Int.ToCsv(separator);
            if (value is Vector3Int vector3Int)
                return vector3Int.ToCsv(separator);
            if (value is Vector4 vector4)
                return vector4.ToCsv(separator);
            if (value is Quaternion quaternion)
                return quaternion.ToCsv(separator);
            if (value is ICsvSupport csvSupport)
                return csvSupport.ToCsv(separator);
            if (value is not string && value is IEnumerable enumerable)
            {
                var array = new List<object>();
                foreach (var data in enumerable)
                    array.Add(data);
                return array.ToCsv(separator);
            }
            return value.ToString();
        }

        public static string ToCsv(this string value) => $"\"{value}\"";
        public static string ToCsv(this Vector2 value, string separator = ",")
            => Format(new object[] { value.x, value.y }, separator);
        public static string ToCsv(this Vector2Int value, string separator = ",")
            => Format(new object[] { value.x, value.y }, separator);
        public static string ToCsv(this Vector3 value, string separator = ",")
            => Format(new object[] { value.x, value.y, value.z }, separator);
        public static string ToCsv(this Vector3Int value, string separator = ",")
            => Format(new object[] { value.x, value.y, value.z }, separator);
        public static string ToCsv(this Vector4 value, string separator = ",")
            => Format(new object[] { value.x, value.y, value.z, value.w }, separator);
        public static string ToCsv(this Quaternion value, string separator = ",")
            => Format(new object[] { value.x, value.y, value.z, value.w }, separator);
        public static string ToCsv<T>(this IEnumerable<T> value, string separator = ",")
            => $"[{string.Join(separator, value.Select(v => ToString(v)))}]";

        public static string FromCsv(this string self)
            => self.Trim('"');

        public static Vector2 ToVector2(this string self, string separator = ",")
        {
            var splited = Split(self, separator);
            if (splited.Length <= 1) throw new InvalidFormatException();
            if (float.TryParse(splited[0], out var x) && float.TryParse(splited[1], out var y))
                return new Vector2(x, y);
            throw new InvalidFormatException();
        }

        public static Vector2Int ToVector2Int(this string self, string separator = ",")
        {
            var splited = Split(self, separator);
            if (splited.Length <= 1) throw new InvalidFormatException();
            if (int.TryParse(splited[0], out var x) && int.TryParse(splited[1], out var y))
                return new Vector2Int(x, y);
            throw new InvalidFormatException();
        }

        public static Vector3 ToVector3(this string self, string separator = ",")
        {
            var splited = Split(self, separator);
            if (splited.Length <= 2) throw new InvalidFormatException();
            if (float.TryParse(splited[0], out var x) && float.TryParse(splited[1], out var y) && float.TryParse(splited[2], out var z))
                return new Vector3(x, y, z);
            throw new InvalidFormatException();
        }

        public static Vector3Int ToVector3Int(this string self, string separator = ",")
        {
            var splited = Split(self, separator);
            if (splited.Length <= 2) throw new InvalidFormatException();
            if (int.TryParse(splited[0], out var x) && int.TryParse(splited[1], out var y) && int.TryParse(splited[2], out var z))
                return new Vector3Int(x, y, z);
            throw new InvalidFormatException();
        }

        public static Vector4 ToVector4(this string self, string seprator = ",")
        {
            var splited = Split(self, seprator);
            if (splited.Length <= 3) throw new InvalidFormatException();
            if (   float.TryParse(splited[0], out var x)
                && float.TryParse(splited[1], out var y)
                && float.TryParse(splited[2], out var z)
                && float.TryParse(splited[3], out var w))
                return new Vector4(x, y, z, w);
            throw new InvalidFormatException();
        }

        public static Quaternion ToQuaternion(this string self, string separator = ",")
        {
            var splited = Split(self, separator);
            if (splited.Length <= 3) throw new InvalidFormatException();
            if (   float.TryParse(splited[0], out var x)
                && float.TryParse(splited[1], out var y)
                && float.TryParse(splited[2], out var z)
                && float.TryParse(splited[3], out var w))
                return new Quaternion(x, y, z, w);
            throw new InvalidFormatException();
        }

        public static List<T> ToList<T>(this string self, string separator = ",")
        {
            var splited = self.Trim('[', ']').Split(separator);
            var type = typeof(T);
            var result = new List<T>();
            foreach (var row in splited)
            {
                if (!ParseFuncDict.TryGetValue(type, out var function))
                {
                    Debug.LogError($"{row} is invalid format at {type}");
                    continue;
                }
                result.Add((T)function.Invoke(row));
            }
            return result;
        }

        private static string Format(object[] values, string separator)
            => $"({string.Join(separator, values)})";
        private static string[] Split(string original, string separator)
            => original.Trim('(', ')').Split(separator);

        private static readonly Dictionary<Type, Func<string, object>> ParseFuncDict = new()
        {
            { typeof(int), text => int.Parse(text) },
            { typeof(float), text => float.Parse(text) },
            { typeof(double), text => double.Parse(text) },
            { typeof(bool), text => bool.Parse(text) },
            { typeof(Vector2), text => text.ToVector2() },
            { typeof(Vector2Int), text => text.ToVector2Int() },
            { typeof(Vector3), text => text.ToVector3() },
            { typeof(Vector3Int), text => text.ToVector3Int() },
            { typeof(Vector4), text => text.ToVector4() },
            { typeof(Quaternion), text => text.ToQuaternion() },
            { typeof(string), text => text.FromCsv() }
        };
    }
}