namespace Administrator.Commands
{
    public sealed class LowercaseAttribute : SanitaryTextAttribute
    {
        public LowercaseAttribute() 
            : base(x => x.ToLowerInvariant())
        { }
    }
}