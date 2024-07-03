using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Xeon.IO;

public class CsvSupportTest
{
    [Test]
    public void Vector2Test()
    {
        var vector2 = new Vector2(10.5f, 2.5f);
        Assert.That(vector2.ToCsv() == "(10.5,2.5)");
        Assert.That("(10,5.5)".ToVector2() == new Vector2(10f, 5.5f));
        Assert.That("1,4".ToVector2() == new Vector2(1,4));
        Assert.Throws<InvalidFormatException>(() => "(1)".ToVector2());
        Assert.Throws<InvalidFormatException>(() => "1".ToVector2());
    }

    [Test]
    public void Vector2IntTest()
    {
        var vector2Int = Vector2Int.one;
        Assert.That(vector2Int.ToCsv() == "(1,1)");
        Assert.That("(4,2)".ToVector2Int() == new Vector2Int(4, 2));
        Assert.That("3,66".ToVector2Int() == new Vector2Int(3, 66));
        Assert.Throws<InvalidFormatException>(() => "(1.1,4)".ToVector2Int());
        Assert.Throws<InvalidFormatException>(() => "(1)".ToVector2Int());
    }

    [Test]
    public void Vector3Test()
    {
        var vector3 = new Vector3(1.1f, 2.2f, 3.3f);
        Assert.That(vector3.ToCsv() == "(1.1,2.2,3.3)");
        Assert.That("(1,2.2,3.5)".ToVector3() == new Vector3(1f, 2.2f, 3.5f));
        Assert.That("4.4,5,6.7".ToVector3() == new Vector3(4.4f, 5f, 6.7f));
        Assert.Throws<InvalidFormatException>(() => "1,2".ToVector3());
    }

    [Test]
    public void Vector3IntTest()
    {
        var vector3Int = new Vector3Int(1, 2, 3);
        Assert.That(vector3Int.ToCsv() == "(1,2,3)");
        Assert.That("(4,5,6)".ToVector3Int() == new Vector3Int(4, 5, 6));
        Assert.That("7,8,9".ToVector3Int() == new Vector3Int(7, 8, 9));
        Assert.Throws<InvalidFormatException>(() => "(1.1,2,3)".ToVector3Int());
        Assert.Throws<InvalidFormatException>(() => "1,1".ToVector3Int());
    }

    [Test]
    public void Vector4Test()
    {
        var vector4 = new Vector4(1.1f, 2.2f, 3.3f, 4f);
        Assert.That(vector4.ToCsv() == "(1.1,2.2,3.3,4)");
        Assert.That("(1,2,3,4.4)".ToVector4() == new Vector4(1f, 2f, 3f, 4.4f));
        Assert.That("1.2,3,4,5".ToVector4() == new Vector4(1.2f, 3f, 4f, 5f));
        Assert.Throws<InvalidFormatException>(() => "(1,2,3)".ToVector4());
    }

    [Test]
    public void QuaternionTest()
    {
        var quaternion = Quaternion.identity;
        Assert.That(quaternion.ToCsv() == "(0,0,0,1)");
        Assert.That("(1,2,3,4.4)".ToQuaternion() == new Quaternion(1f, 2f, 3f, 4.4f));
        Assert.That("1.2,3,4,5".ToQuaternion() == new Quaternion(1.2f, 3f, 4f, 5f));
        Assert.Throws<InvalidFormatException>(() => "(3,2,1)".ToQuaternion());
    }

    [Test]
    public void ListTest()
    {
        var stringList = new List<string>() { "abc", "def", "123" };
        Assert.That(stringList.ToCsv() == "[\"abc\",\"def\",\"123\"]");

        var intList = new List<int>() { 123, 456, 1000 };
        Assert.That(intList.ToCsv() == "[123,456,1000]");

        var floatList = new List<float>() { 1.1f, 2.2f, 3f };
        Assert.That(floatList.ToCsv() == "[1.1,2.2,3]");
    }
}
