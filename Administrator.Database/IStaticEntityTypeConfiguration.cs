using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public interface IStaticEntityTypeConfiguration<TModel> where TModel : class
{
    static abstract void ConfigureBuilder(EntityTypeBuilder<TModel> builder);
}