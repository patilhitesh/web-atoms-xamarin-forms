using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WebAtoms
{
    public static class DictionaryExtensions {

        static Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

        public static TValue GetOrCreate<TKey, TValue>(
            this Dictionary<TKey,TValue> d,
            TKey key,
            Func<TKey, TValue> factory) {
            if (d.TryGetValue(key, out TValue value))
                return value;
            TValue v = factory(key);
            d[key] = v;
            return v;
        }

        public static PropertyInfo GetProperty(this object value, string name) {
            Type type = value.GetType();
            string key = $"{type.FullName}.{name}";

            return properties.GetOrCreate(key, k => {
                var a = type.GetProperties().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (a == null) {
                    throw new KeyNotFoundException($"Properrty {name} not found {type.FullName}");
                }
                return a;
            });
        }

        public static bool IsEmpty(this string text) {
            return string.IsNullOrWhiteSpace(text);
        }
    }

}