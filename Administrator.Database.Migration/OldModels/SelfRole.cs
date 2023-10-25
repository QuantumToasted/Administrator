namespace Administrator.Database.Migration
{
    public partial class SelfRole
    {
        public decimal GuildId { get; set; }
        public decimal RoleId { get; set; }
        public int[]? Groups { get; set; }
    }
}
