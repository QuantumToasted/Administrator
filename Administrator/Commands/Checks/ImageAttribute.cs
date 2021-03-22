namespace Administrator.Commands
{
    public sealed class ImageAttribute : AllowedExtensionsAttribute
    {
        public ImageAttribute()
            : base("png", "gif", "jpeg", "jpg", "bmp")
        { }
    }
}