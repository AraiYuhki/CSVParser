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
  - 但し改行には対応していません。
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

## エスケープ処理の順番

通常はこちらの順番で処理されます。

1. 文字列
2. Vector 系
3. IList
4. ICsvSupport

元にもとに戻す際には、以下の順で処理されます。

1. 文字列
2. ICsvSupport
3. Vector 系
4. IList
   ICsvSupport はこの後更に文字列の復元処理を挟みます。

元々 CSV 形式は配列形式や Vector 系の複数の要素を一つのカラムにまとめることが苦手なので、使わないことに越したことはありません。

## 自作クラスを CSV に書き出すには

原則として`ICsvSupport`を継承し、`ToCsv`,`FromCsv`を実装したクラスが対応可能なクラスとなります。

例えば以下のように記述することで、CSV に書き出すことが可能なクラスを定義することができます。
今回の例では複数の形式の要素を併せ持つクラスを定義しています。

```C#
public class TestValue : ICsvSupport
{
    public int id;
    public string name;
    public Vector3 position = Vector3.zero;

    public string ToCsv(string separator = ",")
    {
        var name = $"\"{this.name}\"";
        return $"{{{string.Join(separator, id, name, position)}}}";
    }
    public void FromCsv(string csv)
    {
        var tmp = csv.Trim('{', '}');
        var match = Regex.Match(tmp, @"(\([\d\s\.\,\-]+\))");
        if (!match.Success)
            throw new InvalidDataException($"position param is ignore format. {csv}");
        tmp = tmp.Replace($",{match.Value}", "");

        var values = match.Value.Trim('(', ')').Split(',');
        position = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));

        var splited = tmp.Split(',');
        id = int.Parse(splited[0]);
        name = string.Join(",", splited.Skip(1).Select(text => text.Trim('"')));
    }
}
```
