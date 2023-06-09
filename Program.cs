﻿using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net;

using System.Diagnostics;

namespace sysprog;
class ExceptionWithStatusCode : Exception
{
    public int StatusCode { get; set; }
    public ExceptionWithStatusCode(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

internal class Program
{

static public void Log(string message)
{
    var st = new StackTrace();
    var sf = st.GetFrame(1);
    string methodName;
    if(sf != null)
    {
        methodName = sf.GetMethod().ToString();
    }
    else methodName = "Place-Holder";

    Console.WriteLine($"{Thread.CurrentThread.Name} --- {methodName}: {message} --- {DateTime.Now}");
} 
static readonly object cachelock = new object();
static SysprogCache cache = new SysprogCache(TimeSpan.FromMinutes(2), 64);

static string search(string query) {
    string url = $"https://api.deezer.com/search?q={query}";
    string h;
    try
    {
        lock(cachelock) {
        
            if(cache.TryGetValue(query,out h)) {
                Log($"{query} was found in cache");
                return h;
            }
        }
        HttpClient hc = new HttpClient();
        var resp = hc.GetAsync(url).Result;
        var content = resp.Content.ReadAsStringAsync().Result;
        if(resp.IsSuccessStatusCode) {
            lock(cachelock) {
                cache[query] = content;
                Log($"{query} was added to cache");
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

static void ShowResult(string Message,
HttpListenerContext context,
int statusCode = 200)
{
    var response = context.Response;
    response.StatusCode = statusCode;
    string responseString = $"<HTML><HEAD><META CHARSET=\"UTF-8\"></HEAD><BODY><pre>{Message}</pre></BODY></HTML>";
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    response.ContentLength64 = buffer.Length;
    System.IO.Stream output = response.OutputStream;
    output.Write(buffer,0,buffer.Length);
    output.Close();
}

static async Task search_cb(object context)
{
    HttpListenerContext listenerContext = (HttpListenerContext) context;
    try
    {
        var request = listenerContext.Request;
        if(request.HttpMethod != "GET")
        {
            throw new ExceptionWithStatusCode("Only GET method allowed", (int)HttpStatusCode.MethodNotAllowed);
        }
        Log($"url: {request.Url}");
        var query = request.QueryString;
        if(query.Count == 0)
        {
            throw new ExceptionWithStatusCode("Query string is empty", (int)HttpStatusCode.BadRequest);
        }
        string queryString = query[0] ?? "";
        var rawJson = search(queryString);
        var serJson = JObject.Parse(rawJson);
        int? count = (int?)serJson["total"];
        if(count.HasValue && count.Value == 0)
        {
            throw new ExceptionWithStatusCode("No results found", (int)HttpStatusCode.NotFound);
        }
        ShowResult(serJson.ToString(), listenerContext);
    }
    catch (ExceptionWithStatusCode e)
    {
        ShowResult(e.Message, listenerContext, e.StatusCode);
        Log($"{e.Message} status code:{e.StatusCode}");
    }
    catch(Exception e)
    {
        ShowResult(e.Message, listenerContext, (int)HttpStatusCode.InternalServerError);
        Log($"{e.Message} status code:{(int)HttpStatusCode.InternalServerError}");
    }
}


   
    
    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "Main Thread";
        HttpListener listener = new HttpListener();
        Log("Created http listener");
        listener.Prefixes.Add("http://127.0.0.1:8080/");
        listener.Start();
        Console.WriteLine("Running at: http://127.0.0.1:8080/");
        while(true)
        {
            var context = listener.GetContext();
            var task = Task.Run(() => search_cb(context));
            task.ContinueWith(t => Console.WriteLine($"{t.Id} Completed"));
        }
    }
}



