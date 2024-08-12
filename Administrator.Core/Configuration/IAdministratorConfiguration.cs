namespace Administrator.Core;

public interface IAdministratorConfiguration<TConfiguration> where TConfiguration : IAdministratorConfiguration<TConfiguration>
{
    static string SectionName => typeof(TConfiguration).Name.Replace("Administrator", string.Empty).Replace("Configuration", string.Empty);
}