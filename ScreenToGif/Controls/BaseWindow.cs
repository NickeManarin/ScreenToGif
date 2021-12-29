using System;
using System.Windows;

namespace ScreenToGif.Controls;

public class BaseWindow : Window
{
    public DateTime CreationIn { get; set; }
    public DateTime NonMinimizedIn { get; set; }
    public DateTime MinimizedIn { get; set; }

    public BaseWindow()
    {
        NonMinimizedIn = CreationIn = DateTime.Now;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
            NonMinimizedIn = DateTime.Now;
        else
            MinimizedIn = DateTime.Now;

        base.OnStateChanged(e);
    }
}