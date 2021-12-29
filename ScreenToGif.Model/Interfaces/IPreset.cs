using ScreenToGif.Domain.Events;

namespace ScreenToGif.Domain.Interfaces
{
    public interface IPreset
    {
        string Title { get; set; }    
        
        string Description { get; set; }

        Task<ValidatedEventArgs> IsValid();
    }
}