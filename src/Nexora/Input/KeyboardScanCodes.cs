using System.Linq;

namespace Nexora.Input;

public static class KeyboardScanCodes
{
    public static readonly int[] MainBlock = Enumerable.Range(0x02, 12)
        .Concat(Enumerable.Range(0x10, 12))
        .Concat(Enumerable.Range(0x1E, 11))
        .Concat(Enumerable.Range(0x2B, 11))
        .Append(0x29)
        .Append(0x56)
        .ToArray();
}
