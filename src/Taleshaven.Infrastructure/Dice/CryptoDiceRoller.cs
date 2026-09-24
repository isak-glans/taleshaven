using System.Security.Cryptography;
using Taleshaven.Core.Dice;

namespace Taleshaven.Infrastructure.Dice;

/// <summary>Kryptografiskt säker och jämnt fördelad slump (krav T-5).</summary>
internal sealed class CryptoDiceRoller : IDiceRoller
{
    public int RollDie(int sides) => RandomNumberGenerator.GetInt32(1, sides + 1);
}
