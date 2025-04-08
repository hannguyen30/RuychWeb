namespace RuychWeb.Repository.Abstract
{
    public interface IDatabaseInitializer
    {
        Task SeedAdminAccountAsync();
    }
}
