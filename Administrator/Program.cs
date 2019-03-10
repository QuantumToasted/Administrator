namespace Administrator
{
    public static class Program
    {
        public static void Main()
            => new Administrator().InitializeAsync().GetAwaiter().GetResult();
    }
}
