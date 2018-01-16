using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;

namespace ScreenToGif.Util
{
    public class HotKeyCollection : IDisposable
    {
        public static HotKeyCollection Default = new HotKeyCollection();

        private readonly IList<HotKey> _hotKeys = new List<HotKey>();


        public IEnumerable<HotKey> HotKeys  => _hotKeys;

        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterHotKey(ModifierKeys modifier, Key key, IntPtr windowsHandle, Action callback)
        {
            _hotKeys.Add(new HotKey(modifier, key, windowsHandle, callback));
        }

        public void Dispose()
        {
            foreach (var hotKey in HotKeys)
            {
                hotKey.Dispose();
            }
        }
    }
}