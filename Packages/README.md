# 使い方

Csv に書き出したいフィールドに`CsvColumn`属性をつけることで書き出されるようになります。
例えば、以下のようなクラスを作成し、適当なデータをリスト型に格納して CSV へ書き出しを行うと以下のようになります。

```C#
public class TestData
{
    [SerializeField, CsvColumn("id")]
    private int id;
    [SerializeField, CsvColumn("name")]
    private string name;
    [SerializeField, CsvColumn("position")]
    private Vector3 position;

    public TestData(int id, string name, Vector3 position)
    {
        this.id = id;
        this.name = name;
        this.position = position;
    }
}

var data = new List<TestData>() {
    new TestData(1, "test1", new Vector3(1, 0, 0)),
    new TestData(2, "test2", new Vector3(0, 1, 0)),
    new TestData(3, "test3", new Vector3(0, 0, 1))
};

CsvParser.ToCSV(data);
```

以下のように書き出されます。

| id  |  name   |     position     |
| :-: | :-----: | :--------------: |
|  1  | "test1" | (1.00,0.00,0.00) |
|  2  | "test2" | (0.00,1.00,0.00) |
|  3  | "test3" | (0.00,0.00,1.00) |

CsvColumn に指定するカラム名はフィールド名と同じでなくても大丈夫です。

## 特殊な型について

以下の型は CSV から変換時にエスケープ処理を挟みます。

- string
- IList
- Vector2, Vector3, Vector4, Vector2Int, Vector3Int, Vector4Int, Quaternion
- ICsvSupport を継承したクラス

それぞれの型は以下のような形式で CSV に書き出され、CSV から取り込み時にはそれぞれの形式であることが前提で処理をします。

- string
  - ダブルクォーテーションで括られます。
  - `"{data}"`
- IList
  - 角括弧で括られ、それぞれの要素はカンマで区切られます。
  - `[{data},{data},...]`
- Vector 系
  - 括弧で括られ、それぞれの要素はカンマで区切られます。
  - `({x},{y},{z})`
- ICsvSupport を継承したクラス
  - 波括弧で括られ、それぞれの要素はカンマで区切られます。
  - `{{メンバー1},{メンバー2}}`

## エスケープ処理について
基本的に`()`や`""`などで括られているカラムを対象に、内部にカンマなどの文字が入っていても正常に読み込めるようにするために、読み込む際にエスケープ処理を行ってから各カラムのデータを処理します。

### 処理方法について
エスケープ対象の文字列を検索する際には正規表現を使わずに、始点と終点を探してその内側の文字列をエスケープするように処理をしています。
例えば、`()`で括られている場合は、まず`(`の場所を探し、その後`)`が見つかった時点で、その間に入っていた文字列を一つのデータとしてエスケープ処理を行います。
そのため`[[],[]]`というような二重配列などには対応していませんが、`{[],[],()}`みたいな入れ子には対応しています。

### 処理順について

通常はこちらの順番で処理されます。

1. 文字列
2. Vector系
3. ICsvSupport
4. IList

元に戻す際には、型に応じて以下のように処理されます。
- 文字列・Vector型
  - それぞれの復元処理のみ行われます。
- ICsvSupport
  - 以下の順番で復元されます
    1. IList
    2. ICsvSupport
    3. Vector系
    4. 文字列
- 配列系
  - 先にIListの復元処理を行った後、各要素を挿入する段階で以下の順番で処理されます。
    1. ICsvSupport
    2. Vector系
    3. 文字列

なお、エスケープされている間は`<escaped string>0</escaped string>`といった文字列に変換されるため、ファイル内に近しい文字列は記述しないでください。

## 自作クラスを CSV に書き出すには

原則として`ICsvSupport`を継承し、`ToCsv`,`FromCsv`を実装したクラスが対応可能なクラスとなります。
`ToCsv`はともかく、`FromCsv`は構造が複雑になればなるほど記述も難しくなるので、あまり複雑なクラスをCSVに対応させることは推奨しません。

例えば以下のように記述することで、CSV に書き出すことが可能なクラスを定義することができます。
今回の例では複数の形式の要素を併せ持つクラスを定義しています。
実際にテストコードで記述されているクラスです。

```C#
[Serializable]
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

// こちらはテスト用のラッパークラスです。
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
}
```
実際のテストコードはこちらです。
```C#
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
```