namespace Administrator.Core;

public interface IAdminService;

public interface IAdminService<TService> : IAdminService where TService : class, IAdminService<TService>;