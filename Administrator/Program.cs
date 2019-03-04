namespace Administrator
{
    public class Program
    {
        public static void Main()
            => new Administrator().InitializeAsync().GetAwaiter().GetResult();
    }
}
