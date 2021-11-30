namespace ScreenToGif.Domain.Enums
{
    public enum StatusReasons : int
    {
        None,
        EmptyProperty,
        InvalidState,
        FileAlreadyExists,
        MissingFfmpeg,
        MissingGifski,
        UploadServiceUnauthorized
    }
}