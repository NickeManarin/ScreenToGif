using System;

namespace ScreenToGif.Util.Exceptions
{
    public class GraphicsConfigurationException : Exception
    {
        public GraphicsConfigurationException(string message, Exception exception) : base(message, exception)
        { }
    }
}