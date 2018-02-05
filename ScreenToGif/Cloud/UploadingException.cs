using System;

namespace ScreenToGif.Cloud
{
    public class UploadingException : Exception
    {
        public UploadingException() : base("Uploading failed")
        {
            
        }

        public UploadingException(string message) : base(message)
        {
        }

        public UploadingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}