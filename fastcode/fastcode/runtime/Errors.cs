using System;

namespace fastcode.runtime
{
    public class AssertionFailedException : Exception
    {
        public AssertionFailedException() : base("A debug assertion has failed.")
        {

        }
    }

    public class UnexpectedStatementException : Exception
    {
        public UnexpectedStatementException(string expected, string statement) : base("FastCode expected \"" + expected + "\", but instead got \"" + statement+"\".")
        {

        }

        public UnexpectedStatementException(string statement): base("FastCode did not expect the keyword \"" + statement + "\".")
        {

        }
    }

    public class InvalidOperandTypeException : Exception
    {
        public InvalidOperandTypeException() : base("The operator cannot be applied with incompatible operand types.")
        {

        }
    }

    public class InvalidComparisonException : Exception
    {
        public InvalidComparisonException() : base("The comparison must be performed between values that share the same type.")
        {

        }
    }

    public class UnidentifiedTokenExcepion : Exception
    {
        public UnidentifiedTokenExcepion() : base("FastCode is unable to identify the following token.")
        {

        }
    }
}
