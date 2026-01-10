namespace H3M.Models;

public class HotAHeader
{
    public byte[] Magic { get; set; } = new byte[4];
    public uint Version { get; set; }
    public bool ScriptingEnabled { get; set; }
    public byte[] Reserved { get; set; } = new byte[23];
}
