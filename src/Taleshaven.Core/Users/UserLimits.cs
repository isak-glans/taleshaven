namespace Taleshaven.Core.Users;

public static class UserLimits
{
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 50;

    /// <summary>Taggen på porträtt som passar som profilbild; väljaren visar dem först (B50).</summary>
    public const string ProfilePortraitTag = "profile";

    /// <summary>Längsta text under "About me" på profilsidan (B52).</summary>
    public const int AboutMaxLength = 2_000;
}
