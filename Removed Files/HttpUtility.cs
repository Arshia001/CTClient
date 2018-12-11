using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRClient
{
    public static class ServerUtility
    {
        //public interface ICancellable
        //{
        //    void Cancel();
        //}

        //class ThreadCancellable : ICancellable
        //{
        //    Thread Th;

        //    public ThreadCancellable(Thread Th)
        //    {
        //        this.Th = Th;
        //    }

        //    public void Cancel()
        //    {
        //        Th.Abort();
        //    }
        //}

        //class HttpGetAsyncThreadParams
        //{
        //    public HttpWebRequest Request;
        //    public Action<string> Callback;
        //}


        public static Uri CreateUri(Uri Uri, Dictionary<string, object> QueryString)
        {
            return new Uri(CreateUrl(Uri, QueryString));
        }

        public static string CreateUrl(Uri Uri, Dictionary<string, object> QueryString)
        {
            StringBuilder FinalUrl = new StringBuilder(Uri.ToString());
            FinalUrl.Append("?");
            foreach (var KV in QueryString)
                FinalUrl.Append(KV.Key).Append("=").Append(Uri.EscapeDataString(KV.Value.ToString())).Append("&");
            FinalUrl.Remove(FinalUrl.Length - 1, 1);
            return FinalUrl.ToString();
        }

        static HttpWebRequest CreateRequest(Uri Uri, Dictionary<string, object> QueryString)
        {
            var Result = WebRequest.Create(CreateUrl(Uri, QueryString)) as HttpWebRequest;
            return Result;
        }

        //public static ICancellable HttpGetAsync(Uri Uri, Dictionary<string, object> QueryString, Action<string> Callback)
        //{
        //    var Th = new Thread(HttpGetAsync_Thread);
        //    Th.Start(new HttpGetAsyncThreadParams()
        //    {
        //        Request = CreateRequest(Uri, QueryString),
        //        Callback = Callback
        //    });
        //    return new ThreadCancellable(Th);
        //}

        //static void HttpGetAsync_Thread(object _Params)
        //{
        //    var Params = _Params as HttpGetAsyncThreadParams;

        //    var Res = Params.Request.GetResponse() as HttpWebResponse;

        //    using (var Reader = new StreamReader(Res.GetResponseStream()))
        //        Params.Callback?.Invoke(Reader.ReadToEnd());
        //}

        public static async Task<string> HttpGetAsync(Uri Uri, Dictionary<string, object> QueryString)
        {
            var Req = CreateRequest(Uri, QueryString);
            var Res = await Req.GetResponseAsync() as HttpWebResponse;

            using (var Reader = new StreamReader(Res.GetResponseStream()))
                return await Reader.ReadToEndAsync();
        }

        public static string HttpGet(Uri Uri, Dictionary<string, object> QueryString)
        {
            var Req = CreateRequest(Uri, QueryString);
            var Res = Req.GetResponse() as HttpWebResponse;

            using (var Reader = new StreamReader(Res.GetResponseStream()))
                return Reader.ReadToEnd();
        }
    }
}