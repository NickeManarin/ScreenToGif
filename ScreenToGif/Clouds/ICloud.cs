using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenToGif.Cloud
{
    public interface ICloud
    {
        /// <summary>
        /// Upload file to cloud
        /// </summary>
        /// <param name="path">Path of file</param>
        /// <param name="cancellationToken"></param>
        /// <param name="progressCallback"></param>
        /// <returns>Uploaded file</returns>
        /// <exception cref="UploadingException"></exception>
        Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null);
    }
}