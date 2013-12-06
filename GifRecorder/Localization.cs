using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScreenToGif.Properties;

namespace ScreenToGif
{
    public class LocalizationString
    {

        public void ChangeStrings(string lang, bool first)
        {
            switch (lang)
            {
                case "en":
                    if (!first) //First Load uses english, so no need to change
                    {
                        ChangeToEnglish();
                    }
                    break;
                case "es":

                    ChangeToSpanish();
                    break;
                case "pt":

                    ChangeToPortuguese();
                    break;
                case "it":

                    ChangeToItalian();
                    break;
                case "ru":

                    ChangeToRussian();
                    break;
                case "fr":

                    ChangeToFrench();
                    break;
                default:
                    if (!first)
                    {
                        ChangeToEnglish();
                    }
                    break;
            }
        }

        #region Functions

        private void ChangeToEnglish()
        {
            
        }

        private void ChangeToSpanish()
        {
            
        }

        private void ChangeToPortuguese()
        {
            
        }

        private void ChangeToItalian()
        {
            
        }

        private void ChangeToRussian()
        {
            
        }

        private void ChangeToFrench()
        {

        }

        #endregion
    }
}
