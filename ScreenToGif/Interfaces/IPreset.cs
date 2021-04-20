using System.Threading.Tasks;
using ScreenToGif.Model.Events;

namespace ScreenToGif.Interfaces
{
    public interface IPreset
    {
        string Title { get; set; }    
        
        string Description { get; set; }

        Task<ValidatedEventArgs> IsValid();
    }
}