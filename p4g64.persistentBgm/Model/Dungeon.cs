using System.Runtime.InteropServices;

namespace p4g64.persistentBgm.Model;

public class Dungeon
{
    public string Name { get; }
    public int StartFloor { get; }
    public int EndFloor { get; }

    public Dungeon(string name, int startFloor, int endFloor)
    {
        Name = name;
        StartFloor = startFloor;
        EndFloor = endFloor;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct AutomaticDungeonTask
    {
        [FieldOffset(0x48)]
        public AutomaticDungeonTaskInfo* Info;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AutomaticDungeonTaskInfo
    {
        [FieldOffset(4)]
        public int CurrentFloor;
    }

}