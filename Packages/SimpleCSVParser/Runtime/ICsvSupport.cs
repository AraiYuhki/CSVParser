namespace Xeon.IO
{
    public interface ICsvSupport
    {
        string ToCsv(string sepalator = ",");
        void FromCsv(string csv);
    }
}
