using ScreenToGif.Util.Extensions;
using System.Security.AccessControl;

namespace ScreenToGif.Util;

public static class MutexList
{
    private static Dictionary<string, Mutex> All { get; set; } = new();

    public static bool IsInUse(string key)
    {
        GC.Collect();

        return Mutex.TryOpenExisting(@"Global\ScreenToGif" + key.Remove("\\"), out var mutex);
    }

    public static void Add(string key)
    {
        if (All.ContainsKey(key))
            Remove(key);

        var mutex = new Mutex(false, @"Global\ScreenToGif" + key.Remove("\\"), out _);

        var sec = new MutexSecurity();
        sec.AddAccessRule(new MutexAccessRule(Environment.UserDomainName + "\\" + Environment.UserName, MutexRights.FullControl, AccessControlType.Allow));

        mutex.SetAccessControl(sec);

        All.Add(key, mutex);
    }

    public static bool Exists(string key) => All.Any(f => f.Key == key);

    public static void Remove(string key)
    {
        var current = All.FirstOrDefault(f => f.Key == key).Value;

        if (current == null)
            return;

        current.Dispose();

        All.Remove(key);

        GC.Collect();

        //var c = IsInUse(key);
    }

    public static void RemoveAll()
    {
        foreach (var mutex in All)
            mutex.Value.Dispose();

        All.Clear();
        GC.Collect();
    }
}