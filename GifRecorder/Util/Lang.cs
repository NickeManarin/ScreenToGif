using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace ScreenToGif.Util
{
    static class Lang
    {
        internal static string GetString(string str, string lang)
        {

            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException("Empty language query string");
            if (string.IsNullOrEmpty(lang)) throw new ArgumentNullException("No language resource given");

            // culture-specific file, i.e. "LangResources.fr"
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenToGif.Properties.Lang" + lang + ".resources");

            //string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            //if (resourceNames.Any())
            //{
            //    //
            //}

            // resource not found, revert to default resource
            if (null == stream)
            {
                stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenToGif.Properties.Resources.resources");
            }

            ResourceReader reader = new ResourceReader(stream);
            IDictionaryEnumerator en = reader.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Key.Equals(str))
                {
                    return en.Value.ToString();
                }
            }

            #region If string is not translated

            stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenToGif.Properties.Resources");

            reader = new ResourceReader(stream);
            IDictionaryEnumerator en1 = reader.GetEnumerator();
            while (en1.MoveNext())
            {
                if (en1.Key.Equals(str))
                {
                    return en1.Value.ToString();
                }
            }

            #endregion

            return "<STRING>";
        }
    }
}
