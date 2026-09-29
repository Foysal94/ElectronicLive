using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ElectronicLive.Api.Models;

public sealed partial record SubscribeRequest(string? Email, string? ArtistName, string? City = "London")
{
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
        {
            errors["email"] = ["A valid email address is required."];
        }

        if (string.IsNullOrWhiteSpace(ArtistName))
        {
            errors["artistName"] = ["Artist name is required."];
        }

        return errors.Count == 0;
    }

    private static bool IsValidEmail(string email)
    {
        var trimmed = email.Trim();
        if (!EmailRegex().IsMatch(trimmed))
        {
            return false;
        }

        try
        {
            var mailAddress = new MailAddress(trimmed);
            return mailAddress.Address == trimmed;
        }
        catch
        {
            return false;
        }
    }

    // RFC 5322 official standard regex pattern for structural email address validation
    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex EmailRegex();
}

public sealed record SubscribeResponse(Guid SubscriptionId, string Message);
