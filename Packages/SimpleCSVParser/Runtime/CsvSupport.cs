using System;
using UnityEngine;

namespace Xeon.IO
{
    public class InvalidFormatException : Exception
    {
        public override string Message => "フォーマットが正しくありません";
    }
    public static class CsvSupport
    {
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

        private static string Format(object[] values, string separator)
            => $"({string.Join(separator, values)})";
        private static string[] Split(string original, string separator)
            => original.Trim('(', ')').Split(separator);
    }
}