using Microsoft.Extensions.Options;
using VendingMachine.Services.Configuration;

namespace VendingMachine.Services.Services;

public class UserService(
    IUserInteractor userInteractor,
    IOptions<UserRolesSettings> userRolesSettings) : IUserService
{
    public string RequestUserRole()
    {
        string? userRole;

        while (true)
        {
            userInteractor.ShowMessage("Are you a customer or a vendor?");

            userRole = userInteractor.ReadInput();

            if (string.IsNullOrEmpty(userRole))
                continue;

            if (!userRolesSettings.Value.AllowedRoles.Contains(
                userRole.ToLowerInvariant(),
                StringComparer.OrdinalIgnoreCase))
            {
                userInteractor.ShowMessage("Invalid user role.");
                continue;
            }

            return userRole;
        }
    }
}
