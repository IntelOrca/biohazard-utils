using System.Collections.Generic;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public class ErrorList
    {
        public List<Error> Errors { get; } = new List<Error>();

        public int Count => Errors.Count;

        public void AddError(string path, int line, int column, int code, string message)
        {
            Errors.Add(new Error(path, line, column, ErrorKind.Error, code, message));
        }

        public void AddWarning(string path, int line, int column, int code, string message)
        {
            Errors.Add(new Error(path, line, column, ErrorKind.Warning, code, message));
        }
    }
}
