using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Controls;

public class AttachmentListBoxItem : ListBoxItem
{
    #region Dependency Property

    public static readonly DependencyProperty ShortNameProperty = DependencyProperty.Register("ShortName", typeof(string), typeof(AttachmentListBoxItem),
        new FrameworkPropertyMetadata("Something", FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty AttachmentProperty = DependencyProperty.Register("Attachment", typeof(string), typeof(AttachmentListBoxItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, AttachmentChangedCallback));

    public static readonly DependencyProperty FileIconProperty = DependencyProperty.Register("FileIcon", typeof(BitmapSource), typeof(AttachmentListBoxItem));

    #endregion

    #region Property Accessor

    [Bindable(true), Category("Common")]
    public string ShortName
    {
        get => (string)GetValue(ShortNameProperty);
        private set => SetValue(ShortNameProperty, value);
    }

    [Bindable(true), Category("Common")]
    public string Attachment
    {
        get => (string)GetValue(AttachmentProperty);
        private set => SetValue(AttachmentProperty, value);
    }

    [Bindable(true), Category("Common")]
    public BitmapSource FileIcon
    {
        get => (BitmapSource)GetValue(FileIconProperty);
        private set => SetValue(FileIconProperty, value);
    }

    #endregion

    static AttachmentListBoxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AttachmentListBoxItem), new FrameworkPropertyMetadata(typeof(AttachmentListBoxItem)));
    }

    public AttachmentListBoxItem(string attachment)
    {
        Attachment = attachment;
    }

    private static void AttachmentChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = d as AttachmentListBoxItem;

        if (item == null)
            return;

        if (!File.Exists(item.Attachment)) return;

        item.ShortName = Path.GetFileName(item.Attachment);

        using (var icon = Icon.ExtractAssociatedIcon(item.Attachment))
        {
            if (icon == null)
                return;

            item.FileIcon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        GC.Collect(1);

        item.UpdateLayout();
    }
}