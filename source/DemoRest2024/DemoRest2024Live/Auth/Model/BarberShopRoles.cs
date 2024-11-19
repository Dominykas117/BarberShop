namespace DemoRest2024Live.Auth.Model
{
    public class BarberShopRoles
    {
        public const string Admin = nameof(Admin);
        public const string BarberShopClient = nameof(BarberShopClient);

        public static readonly IReadOnlyCollection<string> All = new[] { Admin, BarberShopClient };
    }
}
