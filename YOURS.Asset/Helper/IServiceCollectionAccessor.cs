/// <summary>
/// https://stackoverflow.com/questions/49937197/how-to-access-iservicecollection-and-or-iserviceprovider-outside-of-startup-clas
/// </summary>
public interface IServiceCollectionAccessor
{
  IServiceCollection ServiceCollection { get; }
}
public sealed class ServiceCollectionAccessor : IServiceCollectionAccessor
{
  public ServiceCollectionAccessor(IServiceCollection serviceCollection)
  {
    this.ServiceCollection = serviceCollection;
  }
  public IServiceCollection ServiceCollection { get; }
}
