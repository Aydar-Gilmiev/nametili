using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace TravelPlanner.Api.UniversalMethod;

public static class EmailHelper
{
    public static bool IsValid(string email)
    {
        return email.Length <= 254
            && !email.Any(char.IsWhiteSpace)
            && new EmailAddressAttribute().IsValid(email)
            && MailAddress.TryCreate(email, out var address)
            && string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
    }
}
