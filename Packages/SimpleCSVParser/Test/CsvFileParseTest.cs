using Codice.Client.Common.GameUI;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Xeon.IO;

public class CsvFileParseTest
{
    private const string TestDataFile = "Packages/SimpleCSVParser/Test/TestData/TestData.csv";

    private enum CharacterType
    {
        Warrior,
        Archer,
        Mage
    }

    [Serializable]
    private class Equipment : ICsvSupport
    {
        [SerializeField]
        private string weapon;
        [SerializeField]
        private string armor;

        public string Weapon => weapon;
        public string Armor => armor;

        public Equipment(string weapon, string armor)
        {
            this.weapon = weapon;
            this.armor = armor;
        }

        public Equipment() { }

        public void FromCsv(string csv)
        {
            var data = JsonUtility.FromJson<Equipment>(csv);
            weapon = data.weapon;
            armor = data.armor;
        }

        public string ToCsv(string sepalator = ",")
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    private class Attributes
    {
        [SerializeField]
        private int strength;
        [SerializeField]
        private int dexterity;
        [SerializeField]
        private int intelligence;
        [SerializeField]
        private int wisdom;

        public int Strength => strength;
        public int Dexterity => dexterity;
        public int Intelligence => intelligence;
        public int Wisdom => wisdom;
    }

    private class EquipmentDetails
    {
        [SerializeField]
        private float weaponDamage;
        [SerializeField]
        private int armorClass;

        public float WeaponDamage => weaponDamage;
        public int ArmorClass => armorClass;
    }

    private class DetailedInfo : ICsvSupport
    {
        [SerializeField]
        private Attributes attributes;
        [SerializeField]
        private EquipmentDetails equipmentDetails;
        [SerializeField]
        private List<string> inventory = new();

        public Attributes Attributes => attributes;
        public EquipmentDetails EquipmentDetails => equipmentDetails;
        public List<string> Inventory => inventory;

        public void FromCsv(string csv)
        {
            Debug.Log(csv);
            var data = JsonUtility.FromJson<DetailedInfo>(csv);
        }

        public string ToCsv(string sepalator = ",")
        {
            return JsonUtility.ToJson(this);
        }
    }

    private class Character : CsvData
    {
        [CsvColumn("id")]
        private int id;
        [CsvColumn("name")]
        private string name;
        [CsvColumn("level")]
        private int level;
        [CsvColumn("health")]
        private float health;
        [CsvColumn("position")]
        private Vector3 position;
        [CsvColumn("skills")]
        private List<int> skills = new();
        [CsvColumn("equipment")]
        private Equipment equipment;
        [CsvColumn("isActive")]
        private bool isActive;
        [CsvColumn("characterType")]
        private CharacterType characterType;
        [CsvColumn("rotation")]
        private Quaternion rotation;
        [CsvColumn("stats")]
        private Vector4 stats;
        [CsvColumn("detailedInfo")]
        private DetailedInfo detailedInfo;

        public int Id => id;
        public string Name => name;
        public int Level => level;
        public float Health => health;
        public Vector3 Position => position;
        public List<int> Skills => skills;
        public Equipment Equipment => equipment;
        public bool IsActive => isActive;
        public CharacterType CharacterType => characterType;
        public Quaternion Rotation => rotation;
        public Vector4 Stats => stats;
        public DetailedInfo DetailedInfo => detailedInfo;
    }

    [Test]
    public void ParseFileTest()
    {
        var equipments = new Equipment("Iron sword", "Cloth armor");
        equipments.ToCsv();
        var path = Application.dataPath.Replace("Assets", "");
        path = Path.Combine(path, TestDataFile);
        var csv = string.Empty;
        using (var reader = new StreamReader(path))
            csv = reader.ReadToEnd();
        Debug.Log(csv);
        var escapedData = new Dictionary<string, string>();
        csv = CsvUtility.EscapeStringRegex(csv, escapedData);
        Debug.Log(csv);
        csv = CsvUtility.EscapeVectorRegex(csv, escapedData);
        Debug.Log(csv);
        csv = CsvUtility.EscapeObjectRegex(csv, escapedData);
        Debug.Log(csv);
        csv = CsvUtility.EscapeListRegex(csv, escapedData);
        Debug.Log(csv);
        foreach (var (key, value) in escapedData)
        {
            var original = value;
            original = Restore(value, escapedData);
            Debug.Log($"{key} => {original}");
        }
    }

    private string Restore(string text, Dictionary<string, string> escapedData, string escapeTarget)
    {
        var result = text;
        while(true)
        {
            var isReplaced = false;
            foreach (var (escaped, origin) in escapedData)
            {
                if (!escaped.Contains(escapeTarget)) continue;
                if (text.Contains(escaped))
                {
                    result = result.Replace(escaped, origin);
                    isReplaced = true;
                }
            }
            if (!isReplaced)
                return result;
        }
    }

    private string Restore(string text, Dictionary<string, string> escapedData)
    {
        var result = text;
        while (true)
        {
            var isReplaced = false;
            foreach (var (escaped, origin) in escapedData)
            {
                if (result.Contains(escaped))
                {
                    result = result.Replace(escaped, origin);
                    isReplaced = true;
                }
            }
            if (!isReplaced)
                return result;
        }
    }
}
