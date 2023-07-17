namespace IntelOrca.Biohazard.Script.Compilation
{
    public interface IScdGenerator
    {
        ErrorList Errors { get; }
        byte[] OutputInit { get; }
        byte[] OutputMain { get; }

        int Generate(IFileIncluder includer, string path);
    }
}
