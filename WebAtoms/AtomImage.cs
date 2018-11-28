using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace WebAtoms
{
    public class AtomImage: Image
    {

        private static HttpClient _Client;
        public static HttpClient Client => _Client ?? (_Client = DependencyService.Get<IWebClient>().Client);



        #region Property SourceUrl

        /// <summary>
        /// Bindable Property SourceUrl
        /// </summary>
        public static readonly BindableProperty SourceUrlProperty = BindableProperty.Create(
          nameof(SourceUrl),
          typeof(string),
          typeof(AtomImage),
          null,
          BindingMode.OneWay,
          // validate value delegate
          // (sender,value) => true
          null,
          // property changed, delegate
          (sender,oldValue,newValue) => ((AtomImage)sender).OnSourceUrlChanged(oldValue,newValue),
          // null,
          // property changing delegate
          // (sender,oldValue,newValue) => {}
          null,
          // coerce value delegate 
          // (sender,value) => value
          null,
          // create default value delegate
          // () => Default(T)
          null
        );


        /// <summary>
        /// On SourceUrl changed
        /// </summary>
        /// <param name="oldValue">Old Value</param>
        /// <param name="newValue">New Value</param>
        protected virtual void OnSourceUrlChanged(object oldValue, object newValue)
        {
            if (newValue == null)
            {
                return;
            }

            this.Source = new StreamImageSource() { Stream = async (ct) => {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, this.SourceUrl);
                    var res = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!res.IsSuccessStatusCode)
                    {
                        AtomDevice.Instance.Log(LogType.Error, await res.Content.ReadAsStringAsync());
                        return null;
                    }
                    var s = await res.Content.ReadAsStreamAsync();
                    return s;
                }
                catch (TaskCanceledException) {
                    return null;
                }
                catch (Exception ex)
                {
                    AtomDevice.Instance.Log(LogType.Error, ex.ToString());
                    return null;
                }
            } };
        }


        /// <summary>
        /// Property SourceUrl
        /// </summary>
        public string SourceUrl
        {
            get
            {
                return (string)GetValue(SourceUrlProperty);
            }
            set
            {
                SetValue(SourceUrlProperty, value);
            }
        }
        #endregion



    }
}
