using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace ScreenToGif.Model;

[DataContract]
public class FosshubRelease
{
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "date")]
    public string DateString { get; set; }

    public DateTime? CreatedAt
    {
        get
        {
            if (DateTime.TryParse(DateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
                return date;
                
            return null;
        }
    }

    [DataMember(Name = "link")]
    public string Link { get; set; }

    [DataMember(Name = "items")]
    public List<FosshubItem> Items { get; set; }
}