using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ScreenToGif.Util;

public static class Serializer
{
    public static string Serialize<T>(T data)
    {
        using (var ms = new MemoryStream())
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(ms, data);
            ms.Position = 0;

            using (var reader = new StreamReader(ms, Encoding.UTF8))
                return reader.ReadToEnd();
        }
    }

    public static T Deserialize<T>(string json)
    {
        var ser = new DataContractJsonSerializer(typeof(T));

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            return (T) ser.ReadObject(stream);
    }
}