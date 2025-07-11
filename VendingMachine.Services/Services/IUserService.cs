using VendingMachine.Common.Enums;

namespace VendingMachine.Services.Services;

public interface IUserService
{
    public UserRoles RequestUserRole();
}
