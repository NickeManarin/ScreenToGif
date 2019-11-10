namespace ScreenToGif.Controls.Ribbon
{
    public class ItemSizeDefinition
    {
        public enum IconSizeEnum
        {
            Large,
            Small
        }

        public IconSizeEnum IconSize { get; set; } = IconSizeEnum.Large;
        public bool IsHeaderVisible { get; set; } = true;
    }
}