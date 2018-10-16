using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace WebAtoms
{
    public enum LogType
    {
        Log,
        Warning,
        Error
    }

    public class LogEvent: EventArgs
    {
        public string Text { get; set; }

        public LogType LogType { get; set; }
    }

    public class AtomDevice
    {

        public static AtomDevice Instance = new AtomDevice();

        public event EventHandler<LogEvent> LogEvent;

        public void BeginInvokeOnMainThread(params Func<Task>[] funcs)
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
                        Log(LogType.Error, ex.ToString());
                    }
                }
            });
        }

        public void Log(LogType type, string text)
        {
            LogEvent?.Invoke(this, new WebAtoms.LogEvent { LogType = type, Text = text });
        }

    }
}
