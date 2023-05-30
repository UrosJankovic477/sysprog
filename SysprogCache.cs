namespace sysprog;


public class SysprogCacheValue<T>
{
    public DateTime LastModifiedOrRead { get; set; }
    public T Content { get; set; }
    public SysprogCacheValue(T value)
    {
        Content = value;
        LastModifiedOrRead = DateTime.Now;
    }
    public SysprogCacheValue()
    {
        Content = default;
        LastModifiedOrRead = DateTime.Now;
    }
}

public class SysprogCache
{
    public Dictionary<string, SysprogCacheValue<string>> cache {get; protected set;}
    public int Capacity {get;}
    Timer timer;
    TimeSpan timeToLive;
    public SysprogCache(TimeSpan ttl, int capacity)
    {
        cache = new Dictionary<string, SysprogCacheValue<string>>(capacity);
        Capacity = capacity;
        timeToLive = ttl;
        timer = new Timer(_ => Expired(), null, 0, (int)TimeSpan.FromMinutes(1).TotalMilliseconds);
    }
    public void Expired()
    {
        DateTime expiredTime = DateTime.Now.Subtract(timeToLive);
        foreach(var key in cache.Keys)
        {
            if(cache.TryGetValue(key, out var value) && value.LastModifiedOrRead < expiredTime)
            {
                cache.Remove(key);
                Program.Log($"{key} time to live has expired so it was removed from cache");
            }
        }
    }
    public string this[string key]
    {
        get { 
                cache[key].LastModifiedOrRead = DateTime.Now;
                return cache[key].Content; 
            }
        set {
                cache[key] = new SysprogCacheValue<string>(value); 
            }
    }
    public bool TryGetValue(string key, out string? value)
    {
        bool contains = cache.TryGetValue(key, out var cacheValue);
        if(cacheValue == null) value = null;
        else value = cacheValue.Content;
        return contains;
    }
    public void Empty()
    {
        foreach(var key in cache.Keys)
        {
            cache.Remove(key);
        }
        Program.Log("Emptying cache");
    }
}