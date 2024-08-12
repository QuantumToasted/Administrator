using Laylua.Marshaling;

namespace Administrator.Bot;

public interface ILuaModel
{
    static abstract void SetUserDataDescriptor(DefaultUserDataDescriptorProvider provider);
}

public interface ILuaModel<TModel> : ILuaModel
    where TModel : class, ILuaModel
{
    new static void SetUserDataDescriptor(DefaultUserDataDescriptorProvider provider)
        => provider.SetDescriptor<TModel>(new InstanceTypeUserDataDescriptor(typeof(TModel), namingPolicy: CamelCaseUserDataNamingPolicy.Instance));

    static void ILuaModel.SetUserDataDescriptor(DefaultUserDataDescriptorProvider provider) => SetUserDataDescriptor(provider);
}