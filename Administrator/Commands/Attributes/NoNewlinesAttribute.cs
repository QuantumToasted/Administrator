namespace Administrator.Commands.Attributes
{
    public sealed class NoNewlinesAttribute : SanitaryAttribute
    {
        public NoNewlinesAttribute() 
            : base(x => x.Replace("\n", string.Empty))
        { }
    }
}