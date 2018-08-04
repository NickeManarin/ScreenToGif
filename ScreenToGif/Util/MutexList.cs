using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;

namespace ScreenToGif.Util
{
    internal static class MutexList
    {
        private static Dictionary<string, Mutex> All { get; set; } = new Dictionary<string, Mutex>();

        internal static bool IsInUse(string key)
        {
            GC.Collect();

            return Mutex.TryOpenExisting(@"Global\ScreenToGif" + key.Remove("\\"), out var mutex);
        }

        internal static void Add(string key)
        {
            if (All.ContainsKey(key))
                Remove(key);

            var sec = new MutexSecurity();
            sec.AddAccessRule(new MutexAccessRule(Environment.UserDomainName + "\\" + Environment.UserName, MutexRights.FullControl, AccessControlType.Allow));

            var mutex = new Mutex(false, @"Global\ScreenToGif" + key.Remove("\\"), out bool created, sec);

            All.Add(key, mutex);
        }

        internal static bool Exists(string key)
        {
            return All.Any(f => f.Key == key);
        }

        internal static void Remove(string key)
        {
            var current = All.FirstOrDefault(f => f.Key == key).Value;

            if (current == null)
                return;

            current.Dispose();

            All.Remove(key);

            GC.Collect();

            //var c = IsInUse(key);
        }

        internal static void RemoveAll()
        {
            foreach (var mutex in All)
                mutex.Value.Dispose();

            All.Clear();
            GC.Collect();
        }
    }
}