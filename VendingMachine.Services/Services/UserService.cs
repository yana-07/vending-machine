using Spectre.Console;
using VendingMachine.Common.Enums;

namespace VendingMachine.Services.Services;

public class UserService(
    IAnsiConsole ansiConsole)
    : IUserService
{
    public UserRoles RequestUserRole()
    {
        var selecteRole = ansiConsole.Prompt(
            new SelectionPrompt<UserRoles>()
                .Title("Are you a customer or a vendor:")
                .AddChoices(Enum.GetValues<UserRoles>()));

        return selecteRole;
    }
}
