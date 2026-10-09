namespace Taleshaven.Web;

/// <summary>Adresser till användarnas profilsidor (B52).</summary>
public static class UserLinks
{
    public static string Profile(string userId) => $"users/{Uri.EscapeDataString(userId)}";
}
