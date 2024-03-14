using System;

namespace Xeon.IO
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class CsvColumn : Attribute
    {
        private string name = string.Empty;
        public string Name => name;

        public CsvColumn(string name)
        {
            this.name = name;
        }
    }
}
