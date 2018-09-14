using Java.Net;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Java.IO;
using Square.OkHttp3;
using System.Collections.Concurrent;
using Java.Util.Concurrent;
using Xamarin.Forms;
using System.Net;

namespace WebAtoms
{

    public class AppOkHttpClient : IWebClient
    {
        private OkHttpHandler okHandler;

        public AppOkHttpClient()
        {
            this.CookieManager = new WebkitCookieManagerProxy();
            //Android.Webkit.CookieManager.Instance.SetAcceptCookie(true);
            okHandler = new OkHttpHandler();



            Client = new HttpClient(okHandler);


        }

        public HttpClient Client
        {
            get;
        }
        public WebkitCookieManagerProxy CookieManager { get; private set; }

        public string UserAgent
        {
            get;

            set;
        }

        public void ClearCookies()
        {
            CookieManager.Clear();
        }

        public void Dispose()
        {
            try
            {
                Client?.Dispose();
                okHandler?.Dispose();
            }
            catch { }
        }
    }

    public class WebkitCookieManagerProxy : CookieManager
    {

        private Android.Webkit.CookieManager webkitCookieManager;

        public WebkitCookieManagerProxy()
            : this(null, null)
        {
            // Android.Webkit.CookieManager.Instance.SetAcceptCookie(true);
            // Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(null, true);
            CookieHandler.Default = this;
        }

        public WebkitCookieManagerProxy(Java.Net.ICookieStore store, ICookiePolicy policy)
            : base(null, policy)
        {
            this.webkitCookieManager = Android.Webkit.CookieManager.Instance;
        }

        public override void Put(URI uri, IDictionary<string, IList<string>> responseHeaders)
        {
            // make sure our args are valid
            if ((uri == null) || (responseHeaders == null))
                return;

            // save our url once
            String url = uri.ToString();

            // go over the headers
            foreach (String headerKey in responseHeaders.Keys)
            {
                // ignore headers which aren't cookie related
                if ((headerKey == null) || !(headerKey.EqualsIgnoreCase("Set-Cookie2") || headerKey.EqualsIgnoreCase("Set-Cookie"))) continue;

                // process each of the headers
                foreach (String headerValue in responseHeaders[headerKey])
                {
                    var prefs = Android.App.Application.Context.GetSharedPreferences("Cookies", Android.Content.FileCreationMode.Private);
                    prefs.Edit().PutString(uri.ToString(), headerValue).Commit();
                    // this.webkitCookieManager.SetCookie(url, headerValue);
                }
            }
        }


        public override IDictionary<string, IList<string>> Get(URI uri, IDictionary<string, IList<string>> requestHeaders)
        {
            // make sure our args are valid
            if ((uri == null) || (requestHeaders == null)) throw new ArgumentException("Argument is null");

            // save our url once
            String url = uri.ToString();


            // prepare our response
            IDictionary<String, IList<String>> res = new Dictionary<String, IList<String>>();

            CookieContainer cookieContainer = new CookieContainer();

            // get the cookie
            // String cookie = this.webkitCookieManager.GetCookie(url);
            var prefs = Android.App.Application.Context.GetSharedPreferences("Cookies", Android.Content.FileCreationMode.Private);
            foreach (var kvp in prefs.All)
            {
                cookieContainer.SetCookies(new Uri(kvp.Key), kvp.Value.ToString());
            }
            List<string> cookies = new List<string>();
            foreach (System.Net.Cookie cookie in cookieContainer.GetCookies( new Uri( url)))
            {
                cookies.Add(cookie.Name + "=" + cookie.Value);
            }

            if (cookies.Count > 0)
            {
                var hrs = new Android.Runtime.JavaList<string> { string.Join(";", cookies) };
                res["Cookie"] = hrs;
            }


            // return it
            // if (cookie != null)
            //    res["Cookie"] = new Android.Runtime.JavaList<string>() { cookie };

            //Java.Util.IList

            return res;
        }


        internal void Clear()
        {
            this.webkitCookieManager.RemoveAllCookie();
        }
    }

    //public class JavaCookieJar : Java.Lang.Object, ICookieJar
    //{
    //    private CookieHandler cookieHandler;

    //    public JavaCookieJar(CookieHandler cookieHandler)
    //    {
    //        this.cookieHandler = cookieHandler;
    //    }

    //    public IList<Cookie> LoadForRequest(HttpUrl p0)
    //    {
    //        var cookies = new Android.Runtime.JavaList<Cookie>();
    //        try {
    //            var c = cookieHandler.Get(p0.Uri())
    //        } catch (Exception ex) {
    //            System.Diagnostics.Debug.Fail(ex.Message, ex.ToString());
    //        }
    //        return cookies;
    //    }

    //    public void SaveFromResponse(HttpUrl url, IList<Cookie> cookies)
    //    {
    //        Dictionary<string, IList<string>> map = new Dictionary<string, IList<string>>();
    //        foreach (Cookie cookie in cookies)
    //        {
    //            IList<string> list = null;
    //            string name = cookie.Name();
    //            if (!map.TryGetValue(name, out list)) {
    //                list = new Android.Runtime.JavaList<string>();
    //                map[name] = list;
    //            }
    //            list.Add(cookie.Value());
    //        }
    //        try
    //        {
    //            cookieHandler.Put(url.Uri(), map);
    //        }
    //        catch (IOException e)
    //        {
    //            System.Diagnostics.Debug.Fail(e.Message, e.ToString());
    //            //Platform.get().log(WARN, "Saving cookies failed for " + url.resolve("/..."), e);
    //        }
    //    }
    //}

    public class OkHttpHandler : HttpClientHandler
    {

        public readonly OkHttpClient Client;

        public OkHttpHandler()
        {
            var b = new OkHttpClient.Builder();
            b.ConnectTimeout(1, TimeUnit.Minutes).WriteTimeout(1, TimeUnit.Minutes).ReadTimeout(1, TimeUnit.Minutes);
            Square.OkHttp3.Cache cache = new Cache(new File(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/web-cache"), 100 * 1024 * 1024);
            b.Cache(cache);
            this.Client = b.Build();
            this.UseCookies = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Client?.Dispose();
        }

        private ConcurrentDictionary<String, String> cookieCache = new ConcurrentDictionary<string, string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            string url = request.RequestUri.ToString();

            var okRequest = new Request.Builder();
            okRequest.Url(url);

            RequestBody requestBody = null;

            foreach (var header in request.Headers)
            {
                okRequest.AddHeader(header.Key, string.Join(" ", header.Value));
            }

            if (request.Content != null)
            {

                /*foreach (var header in request.Content.Headers)
                {
                    okRequest.AddHeader(header.Key, string.Join(" ", header.Value));
                }*/

                var contentType = MediaType.Parse(request.Content.Headers.ContentType?.ToString() ?? "application/octat-stream");

                using (var ms = new System.IO.MemoryStream())
                {
                    var s = await request.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await s.CopyToAsync(ms).ConfigureAwait(false);

                    requestBody = RequestBody.Create(contentType, ms.ToArray());
                }

            }

            okRequest.Method(request.Method.ToString(), requestBody);

            string cookie = GetCookie(url);
            if (cookie != null)
            {
                okRequest.AddHeader("Cookie", cookie);
            }

            var call = Client.NewCall(okRequest.Build());

            var callBack = new OkHttpCallBack();
            call.Enqueue(callBack);

            var okResponse = await callBack.taskSource.Task.ConfigureAwait(false);

            var response = new HttpResponseMessage();

            response.StatusCode = (System.Net.HttpStatusCode)okResponse.Code();


            List<KeyValuePair<string, string>> contentHeaders = new List<KeyValuePair<string, string>>();

            var rhs = response.Headers;

            var hs = okResponse.Headers();
            int total = hs.Size();
            for (int i = 0; i < total; i++)
            {
                var name = hs.Name(i);
                var value = hs.Value(i);

                if (name.EqualsIgnoreCase("Set-Cookie") || name.EqualsIgnoreCase("Set-Cookie2"))
                {
                    Android.Webkit.CookieManager.Instance.SetCookie(url, value);
                    PrefCookieHandler.Default.SetCookie(url, value);
                    cookieCache.Clear();
                }

                //System.Diagnostics.Debug.Write($"Header: {name} = {value}");
                if (!rhs.TryAddWithoutValidation(name, value))
                {
                    contentHeaders.Add(new KeyValuePair<string, string>(name, value));
                }
            }

            var st = okResponse.Body().ByteStream();
            response.Content = new StreamContent(st);
            foreach (var ch in contentHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(ch.Key, ch.Value);
            }

            //HttpResponseMessage

            return response;

        }

        private string GetCookie(string url)
        {
            return cookieCache.GetOrAdd(url, u => {
                return PrefCookieHandler.Default.Get(url);
            });
        }
    }

    public class PrefCookieHandler {

        private static PrefCookieHandler _Default;

        public static PrefCookieHandler Default => (_Default ?? (_Default = new PrefCookieHandler()));

        public string Get(string url)
        {
            CookieContainer cookieContainer = new CookieContainer();

            // get the cookie
            // String cookie = this.webkitCookieManager.GetCookie(url);
            var prefs = Android.App.Application.Context.GetSharedPreferences("Cookies", Android.Content.FileCreationMode.Private);
            foreach (var kvp in prefs.All)
            {
                cookieContainer.SetCookies(new Uri(kvp.Key), kvp.Value.ToString());
            }
            List<string> cookies = new List<string>();
            foreach (System.Net.Cookie cookie in cookieContainer.GetCookies(new Uri(url)))
            {
                cookies.Add(cookie.Name + "=" + cookie.Value);
            }

            return string.Join(";", cookies);
        }
        public void SetCookie(string url, string value)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("Cookies", Android.Content.FileCreationMode.Private);
            prefs.Edit().PutString(url, value).Commit();
        }

    }

    public class OkHttpCallBack : Java.Lang.Object, ICallback
    {

        public readonly TaskCompletionSource<Response> taskSource = new TaskCompletionSource<Response>();

        public void OnFailure(ICall p0, IOException p1)
        {
            var sw = new Java.IO.StringWriter();
            p1.PrintStackTrace(new PrintWriter(sw));
            taskSource.TrySetException(new OkHttpException(p1.Message + "\r\n" + sw.Buffer.ToString()));
        }

        public void OnResponse(ICall p, Response p0)
        {
            taskSource.TrySetResult(p0);
        }
    }

    public class OkHttpException : Exception
    {

        public OkHttpException(string message) : base(message)
        {
            Device.BeginInvokeOnMainThread( async () =>
            {
                if (message.Contains("java.net.SocketTimeoutException"))
                {
                    await Application.Current.MainPage.DisplayAlert("Timeout", "The server is taking too long to respond. Please try again later.", "Ok");
                    //AtomBridge.Instance.ShowAlert("The server is taking too long to respond. Please try again later.");
                }
                else if (message.Contains("java.net.UnknownHostException"))
                {
                    await Application.Current.MainPage.DisplayAlert("Unable to connect", "Unable to connect to the Internet.", "Ok");
                }
            });
        }
    }

}