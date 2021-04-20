using System;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Model.UploadPresets;
using ScreenToGif.Model.UploadPresets.History;

namespace ScreenToGif.Cloud
{
    public interface IUploader
    {
        /// <summary>
        /// Upload the file to the cloud service.
        /// </summary>
        /// <param name="preset">The upload preset with the configurations necessary for the upload.</param>
        /// <param name="path">Path of file.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="progressCallback"></param>
        /// <returns>The upload details.</returns>
        Task<History> UploadFileAsync(UploadPreset preset, string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null);
    }
}