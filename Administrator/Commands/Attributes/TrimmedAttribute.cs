namespace Administrator.Commands
{
    public sealed class TrimmedAttribute : SanitaryAttribute
    {
        public TrimmedAttribute() 
            : base(x => x.Trim())
        { }
    }
}