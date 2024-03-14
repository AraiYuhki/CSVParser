using UnityEngine.AddressableAssets;

namespace Xeon.IO
{
    public abstract class CsvData
    {
        public virtual void Initialize()
        {
        }

        protected T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            return Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
        }
    }
}
