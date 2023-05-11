using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;

namespace sysprog;
internal class Program
{

static readonly object cachelock = new object();
static Dictionary<string,string> cache = new Dictionary<string, string>();

static string search(string query) {
    string url = $"https://api.deezer.com/search?q={query}";
    string h;
    try
    {
        lock(cachelock) {
        
            if(cache.TryGetValue(query,out h)) {
                Console.WriteLine($"{query} je pronadjen u cache-u");
                return h;
            }
        }
        HttpClient hc = new HttpClient();
    var resp = hc.GetAsync(url).Result;
    var content = resp.Content.ReadAsStringAsync().Result;
    if(resp.IsSuccessStatusCode) {
        lock(cachelock) {
            cache[query] = content;
            }
        return content;
    }
    }
    catch (System.Exception e)
    {

        throw e;
    }
    return h;
}

static void ShowResult(string Message, HttpListenerContext context)
{
    var response = context.Response;
    string responseString = $"<HTML><HEAD><META CHARSET=\"UTF-8\"></HEAD><BODY><pre>{Message}</pre></BODY></HTML>";
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    response.ContentLength64 = buffer.Length;
    System.IO.Stream output = response.OutputStream;
    output.Write(buffer,0,buffer.Length);
    output.Close();
}

static void search_cb(object context)
{
//    Stopwatch stopwatch = new Stopwatch();
//    stopwatch.Start();
    HttpListenerContext listenerContext = (HttpListenerContext) context;
    try
    {
        var request = listenerContext.Request;
        var query = request.QueryString;
        if(query.Count == 0)
        {
            throw new Exception("Nije unet query");
        }
        string queryString = query[0] ?? "";
        var rawJson = search(queryString);
        var serJson = JObject.Parse(rawJson);
        int? count = (int?)serJson["total"];
        if(count.HasValue && count.Value == 0)
        {
            throw new Exception("Nema rezultata");
        }
        ShowResult(serJson.ToString(), listenerContext);
    }
    catch (System.Exception e)
    {
        ShowResult(e.Message, listenerContext);
    }
//    stopwatch.Stop();
//    Console.WriteLine(stopwatch.Elapsed);
}


    static HttpListener listener = new HttpListener();
    static void Main(string[] args)
    {
        listener.Prefixes.Add("http://127.0.0.1:8080/");
        listener.Start();
        Console.WriteLine("Running at: http://127.0.0.1:8080/");
        while(true)
        {
            ThreadPool.QueueUserWorkItem(search_cb, listener.GetContext());
         //   Thread thread = new Thread(new ParameterizedThreadStart(search_cb)); 
         //   thread.Start(listener.GetContext());
        }
    }
}



