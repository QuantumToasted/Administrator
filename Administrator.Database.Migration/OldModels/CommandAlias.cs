namespace Administrator.Database.Migration
{
    public partial class CommandAlias
    {
        public decimal GuildId { get; set; }
        public string Alias { get; set; } = null!;
        public string? Command { get; set; }
    }
}
