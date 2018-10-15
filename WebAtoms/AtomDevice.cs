using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace WebAtoms
{
    public class AtomDevice
    {

        public static void BeginInvokeOnMainThread(params Func<Task>[] funcs)
        {
            Device.BeginInvokeOnMainThread(async () => {
                foreach (var f in funcs)
                {
                    try
                    {
                        await f();
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            });
        }

        public static Action<object> Log = (m) => {
            System.Diagnostics.Debug.WriteLine(m);
        };

    }
}
