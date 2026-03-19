using ScreenToGif.Util;
using ScreenToGif.Windows.Other;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Views.Settings;

public partial class DonateSettings : Page
{
    public DonateSettings()
    {
        InitializeComponent();
    }

    private void DonateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");
            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }

    private void DonateEuroButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");

            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }

    private void DonateOtherButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currency = CurrencyComboBox.Text.Substring(0, 3);

            ProcessHelper.StartWithShell($"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code={currency}&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Donation website");

            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error opening the donation website", ex.Message, ex);
        }
    }

    private void PatreonButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.patreon.com/nicke");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Patreon website");
            ErrorDialog.Ok(Title, "Error opening the Patreon website", ex.Message, ex);
        }
    }

    private void StripeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://donate.stripe.com/cN23dfaz9dJW1wc000");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Stripe website");

            ErrorDialog.Ok(LocalizationHelper.Get("S.Options.Title"), "Error opening the Stripe website", ex.Message, ex);
        }
    }

    private void SteamButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://steamcommunity.com/id/nickesm/wishlist");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Steam website");
            ErrorDialog.Ok(Title, "Error opening the Steam website", ex.Message, ex);
        }
    }

    private void GogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.gog.com/u/Nickesm/wishlist");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the GOG website");
            ErrorDialog.Ok(Title, "Error opening the GOG website", ex.Message, ex);
        }
    }

    private void KofiButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://ko-fi.com/nickemanarin");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the Ko-fi website");
            ErrorDialog.Ok(Title, "Error opening the Ko-fi website", ex.Message, ex);
        }
    }

    private void BitcoinCashCopy_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText("1HN81cAwDo16tRtiYfkzvzFqikQUimM3S8");
    }

    private void MoneroHyperlink_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText("44yC9CkwHVfKPsKxg5RcA67GZEqiQH6QoBYtRKwkhDaE3tvRpiw1E5i6GShZYNsDq9eCtHnq49SrKjF4DG7NwjqWMoMueD4");
    }

    private void SupportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessHelper.StartWithShell("https://www.screentogif.com/donate");
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Opening the donation website");
            ErrorDialog.Ok(Title, "Error opening the donation website", ex.Message, ex);
        }
    }
}