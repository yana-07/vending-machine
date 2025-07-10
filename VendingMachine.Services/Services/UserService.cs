using VendingMachine.Common.Enums;

namespace VendingMachine.Services.Services;

public class UserService(
    IUserInteractor userInteractor)
    : IUserService
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
            if (!Enum.TryParse<UserRoles>(userRole, true, out _))
            {
                userInteractor.ShowMessage("Invalid user role.");
                continue;
            }

            return userRole;
        }
    }
}
