namespace ScreenToGif.Domain.Interfaces
{
    public interface IPanel
    {
        Task<bool> IsValid();
    }
}