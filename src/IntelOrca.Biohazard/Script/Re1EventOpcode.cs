namespace IntelOrca.Biohazard.Script
{
    internal enum Re1EventOpcode : byte
    {
        Unk00,
        Unk01,
        Unk02,
        Unk03,
        Unk04,
        Fork,
        Block,
        Single,
        Unk08,
        Unk86 = 0x86,
        UnkF6 = 0xF6,
        UnkF8 = 0xF8,
        For = 0xFA,
        Next = 0xFB,
        SetInst = 0xFC,
        ExecInst = 0xFD,
        NextEvent = 0xFE,
        Disable = 0xFF,
    }
}
