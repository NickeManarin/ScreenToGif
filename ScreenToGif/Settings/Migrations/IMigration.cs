namespace ScreenToGif.Settings.Migrations
{
    internal interface IMigration
    {
        bool Up(ref string type, ref string property, ref string value);
        bool Down(ref string type, ref string property, ref string value);
    }
}