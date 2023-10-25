namespace Administrator.Database.Migration
{
    public partial class Permission
    {
        public int Id { get; set; }
        public decimal? GuildId { get; set; }
        public int Type { get; set; }
        public bool IsEnabled { get; set; }
        public string? Name { get; set; }
        public int Filter { get; set; }
        public decimal? TargetId { get; set; }
    }
}
