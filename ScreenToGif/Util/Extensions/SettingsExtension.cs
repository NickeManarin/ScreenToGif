using ScreenToGif.Domain.Exceptions;
using ScreenToGif.Util.InterProcessChannel;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows.Other;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace ScreenToGif.Util.Extensions;

internal static class SettingsExtension
{
    internal static void ForceSave()
    {
        try
        {
            UserSettings.Save(true);
        }
        catch (SettingsPersistenceException e)
        {
            Retry(e.ResourceDictionary, e.IsLocal);
        }
    }

    private static async void Retry(ResourceDictionary dic, bool isLocal)
    {
        try
        {
            if (!Dialog.Ask(LocalizationHelper.Get("S.SavingSettings.Title"), LocalizationHelper.Get("S.SavingSettings.Instruction"), LocalizationHelper.Get("S.SavingSettings.Message")))
                return;

            //Get a new instance, but elevated.
            var process = ProcessHelper.RestartAsAdminAdvanced("-settings");
            await Task.Delay(500);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                OmitXmlDeclaration = true,
                CheckCharacters = true,
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                Encoding = Encoding.UTF8
            };

            //Serialize the settings and pass to the new instance via IPC.
            await using var stream = new StringWriter();
            await using var writer = XmlWriter.Create(stream, settings);
            XamlWriter.Save(dic, writer);
            SettingsPersistenceChannel.SendMessage(process.Id, stream.ToString(), isLocal);

            //Since the other instance only exists to save the settings (no interface is displayed), the process must be stopped.
            process.Kill();
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to retry to save the settings.");
        }
    }
}