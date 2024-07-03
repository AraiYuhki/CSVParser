using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Xeon.IO;

public class CsvParseTest
{
    private enum TestEnum
    {
        Zero,
        One,
        Two,
    }

    private class TestValue : ICsvSupport
    {
        public int id;
        public string name;
        public Vector3 position = Vector3.zero;
        public List<int> referenceIds = new();
        public List<Vector3> positions = new();

        public override bool Equals(object obj)
        {
            if (obj is not TestValue other)
                return false;
            if (referenceIds.Count != other.referenceIds.Count) return false;

            foreach (var (referenceId, index) in referenceIds.Select((referenceId, index) => (referenceId, index)))
                if (referenceId != other.referenceIds[index]) return false;

            if (positions.Count != other.positions.Count) return false;
            foreach (var (position, index) in positions.Select((position, index) => (position, index)))
                if (position != other.positions[index]) return false;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(other.name))
                return id == other.id && position == other.position;
            return id == other.id && name == other.name && position == other.position;
        }

        public string ToCsv(string separator = ",")
        {
            var name = $"\"{this.name}\"";
            var referenceIdsText = $"[{string.Join(separator, referenceIds)}]";
            var positionsText = $"[{string.Join(separator, positions)}]";
            return $"{{{string.Join(separator, id, name, position, referenceIdsText, positionsText)}}}";
        }

        public void FromCsv(string csv)
        {
            var tmp = csv.Trim('{', '}');
            name = EscapeName(ref tmp);
            var escapedData = new Dictionary<string, string>();
            tmp = CsvUtility.EscapeVector(tmp, escapedData);
            tmp = CsvUtility.EscapeList(tmp, escapedData);
            var splited = tmp.Split(",");
            id = int.Parse(splited[0]);
            position = escapedData[splited[2]].ToVector3();
            var referenceIdList = escapedData[splited[3]].Trim('[', ']');
            if (string.IsNullOrEmpty(referenceIdList))
                referenceIds = new();
            else
                referenceIds = referenceIdList.Split(',').Select(text => int.Parse(text)).ToList();

            var list = escapedData[splited[4]].Trim('[', ']');
            if (string.IsNullOrEmpty(list))
            {
                positions = new();
                return;
            }
            positions = list.Split(',').Select(text => escapedData[text].ToVector3()).ToList();
        }

        public override string ToString()
        {
            return $"{id}, {name}, {string.Join(',', referenceIds)}, {string.Join(',', positions)}";
        }

        private static string EscapeName(ref string csv)
        {
            var startIndex = csv.IndexOf('"');
            var endIndex = csv.LastIndexOf('"');
            var name = csv.Substring(startIndex, endIndex - startIndex + 1);
            csv = csv.Replace(name, "<escaped>");
            return name.Trim('"');
        }
    }

    [Serializable]
    private class TestData : CsvData
    {
        [CsvColumn("int_value")]
        public int intValue;
        [CsvColumn("float_value")]
        public float floatValue;
        [CsvColumn("string_value")]
        public string stringValue;
        [CsvColumn("bool_value")]
        public bool boolValue;
        [CsvColumn("vector2_value")]
        public Vector2 vector2Value;
        [CsvColumn("vector2int_value")]
        public Vector2Int vector2IntValue;
        [CsvColumn("vector3_value")]
        public Vector3 vector3Value;
        [CsvColumn("vector3int_value")]
        public Vector3Int vector3IntValue;
        [CsvColumn("vector4_value")]
        public Vector4 vector4Value;
        [CsvColumn("enum_value")]
        public TestEnum enumValue;
        [CsvColumn("private_int_value")]
        private int privateValue;

        public TestData() { }
        public TestData(int privateValue)
            => this.privateValue = privateValue;
    }

    [Serializable]
    private class TestData2 : CsvData
    {
        [CsvColumn("list_value")]
        public List<string> listValue = new();
        [CsvColumn("class_value")]
        public TestValue classValue;

        public override bool Equals(object obj)
        {
            if (obj is not TestData2 other)
                return false;
            if (listValue == null || other.listValue == null)
                return false;
            if (listValue.Count != other.listValue.Count)
                return false;
            foreach (var (item, index) in listValue.Select((item, index) => (item, index)))
            {
                if (item != other.listValue[index])
                    return false;
            }
            if (classValue == null && other.classValue == null) return true;
            if (classValue != null && other.classValue != null)
                return classValue.Equals(other.classValue);
            return false;
        }

        public override string ToString()
        {
            var list = string.Join(",", listValue);
            var classText = $"{classValue.ToString()}";
            return $"listValue:{list}, classValue:{classText}";
        }
    }

    [Test]
    public void ToCsvTest()
    {
        var testData = new TestData(12)
        {
            intValue = 10,
            floatValue = 123.45f,
            stringValue = "abcdefg",
            boolValue = false,
            vector2Value = new Vector2(10f, 10.5f),
            vector2IntValue = Vector2Int.down,
            vector3Value = new Vector3(1.1f, 2.2f, 3.3f),
            vector3IntValue = Vector3Int.one,
            vector4Value = new Vector4(1.1f, 2.2f, 3.3f, 4),
            enumValue = TestEnum.Two
        };
        var csv = CsvParser.ToCSV(new List<TestData>() { testData });
        var expect = @"int_value,float_value,string_value,bool_value,vector2_value,vector2int_value,vector3_value,vector3int_value,vector4_value,enum_value,private_int_value
10,123.45,""abcdefg"",False,(10,10.5),(0,-1),(1.1,2.2,3.3),(1,1,1),(1.1,2.2,3.3,4),Two,12
";
        Assert.That(expect == csv);
    }

    [Test]
    public void ParseFileTest()
    {
        var path = Application.dataPath.Replace("Assets", "");
        path = Path.Combine(path, "Packages/SimpleCSVParser/Test/TestData/Test.csv");
        var obj = CsvParser.ParseFile<TestData>(path);
        Debug.Log(obj.Count);
    }

    [Test]
    public void ParseTest()
    {
        var csv = @"int_value,float_value,string_value,bool_value,vector2_value,vector2int_value,vector3_value,vector3int_value,vector4_value,enum_value,private_int_value
10,123.45,""abcdefg"",False,(10,10.5),(0,-1),(1.1,2.2,3.3),(1,1,1),(1.1,2.2,3.3,4),Two,12
";
        var obj = CsvParser.Parse<TestData>(csv);
        Debug.Log(obj.Count);
        Assert.That(obj.Count == 1);
        var data = obj.First();
        Assert.That(data.intValue == 10);
        Assert.That(data.floatValue == 123.45f);
        Assert.That(data.stringValue == "abcdefg");
        Assert.That(data.boolValue == false);
        Assert.That(data.vector2Value == new Vector2(10f, 10.5f));
        Assert.That(data.vector2IntValue == Vector2Int.down);
        Assert.That(data.vector3Value == new Vector3(1.1f, 2.2f, 3.3f));
        Assert.That(data.vector3IntValue == Vector3Int.one);
        Assert.That(data.vector4Value == new Vector4(1.1f, 2.2f, 3.3f, 4));
        Assert.That(data.enumValue == TestEnum.Two);
    }

    [Test]
    public void ListAndClassParseTest()
    {
        var obj = new List<TestData2>(){
            new TestData2() {
                listValue = new() { "abd", "xeon", "tesst" },
                classValue = new TestValue() {
                    id = -1,
                    name = "TestValue" ,
                    position = new Vector3(10.2f, -20.3f, 0),
                    referenceIds = new List<int>(){ 1, 2, 3, 4, 5 },
                    positions = new (){ new Vector3(1, 0, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 1) }
                },
            },
            new TestData2()
            {
                listValue = new(){"array1,array2", "test"},
                classValue = new TestValue(){ 
                    id = 10,
                    name = "test,test2",
                    position = new Vector3(1f, 2f, 3f),
                    referenceIds = new (){ 2, 4, 6, 8},
                }
            },
            new TestData2()
            {
                listValue = new(),
                classValue = new TestValue()
                {
                    positions = new(){ Vector3.one, Vector3.zero, Vector3.forward },
                }
            }
        };

        var csvExpect = @"list_value,class_value
[""abd"",""xeon"",""tesst""],{-1,""TestValue"",(10.20, -20.30, 0.00),[1,2,3,4,5],[(1.00, 0.00, 0.00),(2.00, 0.00, 0.00),(0.00, 0.00, 1.00)]}
[""array1,array2"",""test""],{10,""test,test2"",(1.00, 2.00, 3.00),[2,4,6,8],[]}
[],{0,"""",(0.00, 0.00, 0.00),[],[(1.00, 1.00, 1.00),(0.00, 0.00, 0.00),(0.00, 0.00, 1.00)]}
";
        var csv = CsvParser.ToCSV(obj);
        Assert.That(csv == csvExpect);
        var actual = CsvParser.Parse<TestData2>(csv);
        Assert.That(obj.Count == actual.Count);
        foreach (var (data, index) in actual.Select((data, index) => (data, index)))
        {
            Assert.That(data.Equals(obj[index]));
        }
    }
}
