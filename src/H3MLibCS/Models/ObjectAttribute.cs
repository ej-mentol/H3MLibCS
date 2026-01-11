namespace H3M.Models;

public class ObjectAttribute
{
    public string Def { get; set; } = string.Empty;
    public byte[] Passable { get; set; } = new byte[6];
    public byte[] Active { get; set; } = new byte[6];
    public ushort AllowedLandscapes { get; set; }
    public ushort LandscapeGroup { get; set; }
    public uint ObjectClass { get; set; }
    public uint ObjectNumber { get; set; }
    public byte ObjectGroup { get; set; }
    public byte Above { get; set; }

    // Helper to get mask as a 2D array [row, col] (6x8)
    // 0 = blocked, 1 = passable in Passable mask
    // 1 = visitable in Active mask
    public bool[,] GetPassabilityMatrix() => ParseMask(Passable);
    public bool[,] GetInteractionMatrix() => ParseMask(Active);

    private bool[,] ParseMask(byte[] mask)
    {
        var matrix = new bool[6, 8];
        for (int i = 0; i < 6; i++) // rows
        {
            for (int j = 0; j < 8; j++) // columns
            {
                // VCMI: usedTiles[5 - i][7 - j]
                // We'll use [i, j] where i is row (0-5), j is column (0-7)
                // Bit j of byte i
                matrix[i, j] = ((mask[i] >> j) & 1) != 0;
            }
        }
        return matrix;
    }
}
