using UnityEngine.AddressableAssets;

namespace Xeon.IO
{
    public abstract class CsvData
    {
        public virtual void Initialize()
        {
        }
#if XEON_CSV_PARSER_ADDRESSABLE_SUPPORT
        protected T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            return Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
        }
#endif
    }
}
