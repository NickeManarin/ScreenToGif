using ScreenToGif.Domain.Models.Upload.Imgur;

namespace ScreenToGif.ViewModel.UploadPresets.Imgur;

public class ImgurAlbum
{
    public string Id { get; set; }
        
    public string Title { get; set; }
        
    public string Description { get; set; }
        
    public string Link { get; set; }
        
    public string Privacy { get; set; }
        
    public bool Favorite { get; set; }
        
    public bool? Nsfw { get; set; }

    public int ImagesCount { get; set; }

    public ImgurAlbum()
    { }

    public ImgurAlbum(ImgurAlbumData data)
    {
        Id = data.Id;
        Title = data.Title;
        Description = data.Description;
        Link = data.Link;
        Privacy = data.Privacy;
        Favorite = data.Favorite;
        Nsfw = data.Nsfw;
        ImagesCount = data.ImagesCount;
    }
}