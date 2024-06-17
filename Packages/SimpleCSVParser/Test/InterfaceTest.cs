using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Xeon.IO;

public class InterfaceTest
{
    private class TestValue : ICsvSupport
    {
        private float x;
        private float y;
        private float width;
        private float height;

        public float X => x;
        public float Y => y;
        public float Width => width;
        public float Height => height;

        public override bool Equals(object obj)
        {
            if (obj is not TestValue other)
                return false;
            return x == other.x && y == other.y && width == other.width && height == other.height;
        }

        public TestValue() { }

        public TestValue(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public string ToCsv(string separator = ",")
        {
            return $"{{{string.Join(separator, x, y, width, height)}}}";
        }

        public void FromCsv(string csv)
        {
            var tmp = csv.Trim('{', '}');
            var splited = tmp.Split(',');
            x = float.Parse(splited[0]);
            y = float.Parse(splited[1]);
            width = float.Parse(splited[2]);
            height = float.Parse(splited[3]);
        }

        public override string ToString()
        {
            return $"x:{x},y:{y},width:{width},height:{height}";
        }
    }

    private class TestData : CsvData
    {
        [CsvColumn("id")]
        private int id;
        [CsvColumn("value")]
        private TestValue value;

        public int Id => id;
        public TestValue Value => value;

        public TestData() { }
        public TestData(int id, TestValue value)
        {
            this.id = id;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{id}, {value}";
        }

        public override bool Equals(object obj)
        {
            if (obj is not TestData other)
                return false;
            return id == other.id && value.Equals(other.value);
        }
    }

    [Test]
    public void ToCsvTest()
    {
        var data = new List<TestData>(){
            new TestData(1, new TestValue(-1.2f, 42f, 20.1f, 3.54f)),
            new TestData(10, new TestValue(1, 1, 1, 1))
        };
        var actual = CsvParser.ToCSV(data, ",");
        var expect = @"id,value
1,{-1.2,42,20.1,3.54}
10,{1,1,1,1}
";
        Assert.That(actual == expect);
    }

    [Test]
    public void FromCsvTest()
    {
        var csv = @"id,value
1,{-1.2,42,20.1,3.54}
10,{1,1,1,1}
";
        var data = CsvParser.Parse<TestData>(csv, ",");
        Assert.That(data[0].Equals(new TestData(1, new TestValue(-1.2f, 42f, 20.1f, 3.54f))));
        Assert.That(data[1].Equals(new TestData(10, new TestValue(1, 1, 1, 1))));

    }
}
