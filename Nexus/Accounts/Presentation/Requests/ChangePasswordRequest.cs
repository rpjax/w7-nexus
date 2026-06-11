namespace Nexus.Accounts.Presentation.Requests;

public class ChangePasswordRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
