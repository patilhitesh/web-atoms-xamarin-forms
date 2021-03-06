﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Org.Liquidplayer.Javascript;
using Xamarin.Forms;

namespace WebAtoms
{
    public class AjaxService
    {

        public static AjaxService Instance = new AjaxService();
        private HttpContent CreateContent(JSObject ajaxOptions)
        {
            var data = ajaxOptions.GetJSPropertyValue("data");
            if (data == null)
                return null;
            if ((bool)data.IsString()) {
                string ct = ajaxOptions.GetJSPropertyValue("contentType")?.ToString();
                return new StringContent(data.ToString(), System.Text.Encoding.UTF8, ct ?? "application/octat-stream");
            }
            throw new NotSupportedException();
        }
        public void Invoke(HttpClient client, string url, JSObject ajaxOptions, JSFunction success, JSFunction failed, JSFunction progress)
        {
            Device.BeginInvokeOnMainThread(async () => {
                try {
                    string method = ajaxOptions.GetJSPropertyValue("method").ToString().ToLower();
                    var m = HttpMethod.Get;
                    HttpContent hc = null;
                    switch (method)
                    {
                        case "post":
                            m = HttpMethod.Post;
                            hc = CreateContent(ajaxOptions);
                            break;
                        case "put":
                            m = HttpMethod.Put;
                            hc = CreateContent(ajaxOptions);
                            break;
                        case "delete":
                            m = HttpMethod.Delete;
                            hc = CreateContent(ajaxOptions);
                            break;
                        case "head":
                            m = HttpMethod.Head;
                            break;
                        case "options":
                            m = HttpMethod.Options;
                            break;
                    }

                    var msg = new HttpRequestMessage(m, url);
                    msg.Content = hc;
                    var res = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);

                    ajaxOptions.SetJSPropertyValue("status", (int)res.StatusCode);
                    var ct = res.Content.Headers.ContentType?.ToString();
                    if(ct != null) {
                        ajaxOptions.SetJSPropertyValue("responseType", ct);
                    }

                    //if (!res.IsSuccessStatusCode) {
                    //    string error = await res.Content.ReadAsStringAsync();
                    //    ajaxOptions.SetJSPropertyValue("responseText", error);
                    //    success.Call(null, new Java.Lang.Object[] { error });
                    //    return;
                    //}

                    string text = await res.Content.ReadAsStringAsync();
                    ajaxOptions.SetJSPropertyValue("responseText", text);
                    success.Call(null, new Java.Lang.Object[] { ajaxOptions });
                }
                catch (Exception ex) {
                    failed.Call(null, new Java.Lang.Object[] { ex.ToString() });
                }
            });
        }

    }
}