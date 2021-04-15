using System.Threading.Tasks;

namespace ScreenToGif.Interfaces
{
    public interface IPanel
    {
        Task<bool> IsValid();
    }
}