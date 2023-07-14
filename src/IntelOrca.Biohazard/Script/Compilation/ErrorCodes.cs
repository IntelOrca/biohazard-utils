namespace IntelOrca.Biohazard.Script.Compilation
{
    internal class ErrorCodes
    {
        public const int ScdVersionNotSpecified = 1;
        public const int OpcodeNotInProcedure = 2;
        public const int LabelAlreadyDefined = 3;
        public const int UnknownOpcode = 4;
        public const int ExpectedOpcode = 5;
        public const int ExpectedComma = 6;
        public const int UnknownSymbol = 7;
        public const int TooManyOperands = 8;
        public const int IncorrectNumberOfOperands = 9;
        public const int UnknownLabel = 10;
        public const int InvalidOperator = 11;
        public const int ExpectedProcedureName = 12;
        public const int ExpectedScdVersionNumber = 13;
        public const int InvalidScdVersionNumber = 14;
        public const int ScdVersionAlreadySpecified = 15;
        public const int ProcedureNotValid = 16;
        public const int ScdTypeAlreadySpecified = 17;
        public const int UnknownDirective = 18;
        public const int ExpectedOperator = 19;
        public const int ProcedureNameAlreadyDefined = 20;

        public static string GetMessage(int code) => _messages[code];

        private static readonly string[] _messages = new string[]
        {
                "",
                "SCD version must be specified before any procedure or opcode.",
                "Opcode must be inside a procedure.",
                "'{0}' has already been defined as a label.",
                "'{0}' it not a valid opcode.",
                "Expected opcode.",
                "Expected , after opcode.",
                "'{0}' has not been defined as a constant.",
                "Too many operands for this opcode.",
                "Incorrect number of operands for opcode.",
                "'{0}' has not been defined as a label within the same procedure.",
                "'{0}' is not a known or valid operator.",
                "Expected procedure name.",
                "Expected SCD version number.",
                "Invalid SCD version number. Only version 1, 2, and 3 are supported.",
                "SCD version already specified.",
                "Procedures are not valid in SCD version 1.",
                "SCD type already specified.",
                "'{0}' is not a valid directive.",
                "Expected operator.",
                "The name '{0}' has already been defined as a procedure.",
        };
    }
}
