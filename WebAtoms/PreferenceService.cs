using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace WebAtoms
{
    public interface IJSService
    {
    }


    public class PreferenceService: IJSService
    {

        private bool Get<T>(string name, out T value)
        {
            value = default(T);
            if (Application.Current.Properties.TryGetValue(name, out object v)) {
                value = JsonConvert.DeserializeObject<T>(v.ToString());
                return true;
            };
            return false;
        }

        private void Set<T>(string name, T value) {
            Device.BeginInvokeOnMainThread(async () => {
                Application.Current.Properties[name] = JsonConvert.SerializeObject(value);
                await Application.Current.SavePropertiesAsync();
            });
        }

        public void SetString(string name, string value) {
            Set(name, value);
        }

        public string GetString(string name) {
            return Get<string>(name, out var v) ? v : null;
        }

        public void SetLong(string name, long value)
        {
            Set(name, value);
        }

        public long? GetLong(string name)
        {
            return Get<long?>(name, out var v) ? v : null;
        }

        public void SetDecimal(string name, decimal value)
        {
            Set(name, value);
        }

        public decimal? GetDouble(string name)
        {
            return Get<decimal?>(name, out var v) ? v : null;
        }

        public void SetBool(string name, bool value)
        {
            Set(name, value);
        }

        public bool? GetBool(string name)
        {
            return Get<bool?>(name, out var v) ? v : null;
        }
    }
}
