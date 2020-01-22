namespace Administrator.Commands
{
    public sealed class ReplaceAttribute : SanitaryAttribute
    {
        public ReplaceAttribute(char oldValue, char newValue)
            : base(x => x.Replace(newValue, oldValue))
        { }

        public ReplaceAttribute(string oldValue, string newValue) 
            : base(x => x.Replace(oldValue, newValue))
        { }
    }
}