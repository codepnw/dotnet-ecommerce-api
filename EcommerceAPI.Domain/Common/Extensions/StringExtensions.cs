using System.Text.RegularExpressions;

namespace EcommerceAPI.Domain.Common.Extensions;

public static class StringExtensions
{
    public static string GenerateSlug(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var slug = text.ToLowerInvariant();

        // a-z, 0-9, ก-ฮ, -
        slug = Regex.Replace(slug, @"[^a-z0-9\s-ก-ฮ]", "");
        
        // Replace white space = -
        slug = Regex.Replace(slug, @"\s+", "-");

        slug = slug.Trim('-');

        return slug;
    }
}