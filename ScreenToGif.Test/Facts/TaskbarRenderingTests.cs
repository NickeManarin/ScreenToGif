using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using Xunit;

namespace ScreenToGif.Test.Facts;

public class TaskbarRenderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RapidTaskbarChangesRepaintNestedContent(bool recorder)
    {
        RunOnStaThread(() =>
        {
            var content = new RenderCounter();
            var window = CreateWindow(recorder, content);

            try
            {
                window.Show();
                DrainDispatcher();
                var initialRenders = content.RenderCount;
                Assert.True(initialRenders > 0);

                for (var index = 0; index < 20; index++)
                    window.ShowInTaskbar = index % 2 != 0;

                window.ShowInTaskbar = false;
                Assert.True(TaskbarHelper.GetIsUpdating(window));
                DrainDispatcher();

                Assert.False(window.ShowInTaskbar);
                Assert.False(TaskbarHelper.GetIsUpdating(window));
                Assert.True(content.RenderCount > initialRenders, "Unchanged child content must repaint after the native window is re-shown.");

                var rendersAfterBurst = content.RenderCount;
                window.ShowInTaskbar = true;
                DrainDispatcher();

                Assert.True(window.ShowInTaskbar);
                Assert.True(content.RenderCount > rendersAfterBurst, "Later changes must still schedule a repaint.");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingWindowDuringTaskbarChangeIsSafe(bool recorder)
    {
        RunOnStaThread(() =>
        {
            var window = CreateWindow(recorder, new RenderCounter());
            window.Show();
            DrainDispatcher();

            window.ShowInTaskbar = false;
            window.Close();
            DrainDispatcher();
        });
    }

    private static Window CreateWindow(bool recorder, UIElement content)
    {
        Window window = recorder ? new BaseRecorder() : new ExWindow();
        window.OverridesDefaultStyle = true;
        window.Template = new ControlTemplate(typeof(Window))
        {
            VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
        };
        window.Width = 240;
        window.Height = 160;
        window.ShowActivated = false;
        window.Content = new Border { Child = new Border { Child = content } };
        return window;
    }

    private static void DrainDispatcher() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static void RunOnStaThread(Action action)
    {
        Exception error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }) { IsBackground = true };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The window rendering test did not finish.");

        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }

    private sealed class RenderCounter : FrameworkElement
    {
        public int RenderCount { get; private set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            RenderCount++;
            drawingContext.DrawRectangle(Brushes.CornflowerBlue, null, new Rect(RenderSize));
        }
    }
}