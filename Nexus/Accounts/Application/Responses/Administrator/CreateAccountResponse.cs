using Nexus.Accounts.Application.Responses.Administrator.Models;

namespace Nexus.Accounts.Application.Responses.Administrator;

public class CreateAccountResponse
{
    public AccountDetails Account { get; init; } = default!;
}
