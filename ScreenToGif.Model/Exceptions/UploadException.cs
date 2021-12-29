using System;

namespace ScreenToGif.Cloud
{
    public class UploadException : Exception
    {
        public UploadException() : base("Uploading failed")
        { }

        public UploadException(string message) : base(message)
        { }

        public UploadException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}