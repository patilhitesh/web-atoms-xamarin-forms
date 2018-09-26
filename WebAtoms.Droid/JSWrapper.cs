using Org.Liquidplayer.Javascript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace WebAtoms
{
    public class JSWrapper
    {
        public WeakReference Target { get; }
        public string Key { get; }

        public JSWrapper(WeakReference target, string key)
        {
            this.Target = target;
            this.Key = key;
        }

        internal T As<T>()
        {
            try
            {
                return (T)(Target.IsAlive ? Target.Target : throw new ObjectDisposedException("Object disposed"));
            }
            catch (InvalidCastException ex) {
                throw new InvalidCastException($"Unable to cast from {Target.Target.GetType().FullName} to {typeof(T).FullName}", ex);
            }
        }

        public static void Clear() {
            wrappers.Clear();
        }

        private static Dictionary<string, JSWrapper> wrappers = new Dictionary<string, JSWrapper>();
        private static long id = 1;

        static JSWrapper() {

            // collect garbage
            Device.BeginInvokeOnMainThread(async () => {
                while (true)
                {
                    await Task.Delay(60000);

                    foreach (var wrapper in wrappers.ToList()) {
                        if (!wrapper.Value.Target.IsAlive) {
                            wrappers.Remove(wrapper.Key);
                        }
                    }
                   
                }
            });
        }

        public static JSWrapper Register(object obj) {
            if (obj == null)
                throw new ArgumentNullException("Cannot register null");
            if (obj is JSWrapper w) {
                return w;
            }
            JSWrapper wrapper = null;
            string key = null;
            if (obj is Element e) {
                key = WAContext.GetJSRefKey(e);
                if (key == null)
                {
                    key = GenerateKey(obj);
                    WAContext.SetJSRefKey(e, key);
                    lock (wrappers)
                    {
                        wrappers[key] = wrapper = new JSWrapper(new WeakReference(obj), key);
                    }
                }
                else {
                    wrapper = wrappers[key];
                }
                return wrapper;
            }
            lock (wrappers)
            {
                key = GenerateKey(obj, true);
                wrappers[key] = wrapper = new JSWrapper(new WeakReference(obj), key);
            }
            return wrapper;
        }

        private static string GenerateKey(object obj, bool checkExisting = false)
        {
            if (checkExisting) {
                string found = null;
                var remove = new List<string>();
                foreach (var v in wrappers) {
                    if (!v.Value.Target.IsAlive) {
                        remove.Add(v.Key);
                        continue;
                    }
                    if (v.Value.Target.Target == obj) {
                        found = v.Key;
                        break;
                    }
                }
                foreach (var item in remove) {
                    wrappers.Remove(item);
                }
                if (found != null) {
                    return found;
                }
            }
            return $"{obj.GetHashCode()}:{System.Threading.Interlocked.Add(ref id, 1)}";
        }

        public static JSWrapper FromKey(string key) {
            if (wrappers.TryGetValue(key, out JSWrapper jv)) {
                var t = jv.Target;
                if (t.IsAlive) {
                    return jv;
                }
                wrappers.Remove(key);
            }
            throw new ObjectDisposedException($"No object found for key {key}");
        }
    }
}