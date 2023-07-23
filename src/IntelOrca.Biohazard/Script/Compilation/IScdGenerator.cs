namespace IntelOrca.Biohazard.Script.Compilation
{
    public interface IScdGenerator
    {
        ErrorList Errors { get; }
        byte[] OutputInit { get; }
        byte[] OutputMain { get; }
        string?[] Messages { get; }
        RdtAnimation?[] Animations { get; }

        int Generate(IFileIncluder includer, string path);
    }
}
