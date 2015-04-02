using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Serializer class. Converts object to binary string and vice-versa.
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Converts a give string to an object.
        /// </summary>
        /// <typeparam name="T">The type of the return object.</typeparam>
        /// <param name="settings">The string to convert.</param>
        /// <returns>The converted object.</returns>
        public static T DeserializeFromString<T>(string settings)
        {
            byte[] b = Convert.FromBase64String(settings);
            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Converts the given object to a binary string.
        /// </summary>
        /// <typeparam name="T">The Type of the object.</typeparam>
        /// <param name="settings">The Object to convert.</param>
        /// <returns>A string representation of the</returns>
        public static string SerializeToString<T>(T settings)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, settings);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }
    }
}
