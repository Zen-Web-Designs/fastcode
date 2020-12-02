using fastcode.parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fastcode.runtime
{
    class ForFrame : CallFrame
    {
        public int currentIndex { get; set; }
        public List<Value> Values { get; set; }
        public string IndexerIdentifier { get; set; }

        public ForFrame() : base()
        {
            
        }
    }

    class WhileFrame : CallFrame
    {
        public Marker ExpressionMarker { get; set; } //for control structures set to evaluate

        public WhileFrame() : base()
        {

        }
    }

    class FunctionFrame : CallFrame
    {
        public string Identifier { get; private set; }
        List<string> argument_identifiers;
        public Dictionary<string, Value> LocalVariables { get; private set; }
        public int ExpectedArguments { get; private set; }
        public Marker ReturnPosition { get; set; }
        public Value ReturnResult { get; set; }

        public Queue<Value> functionResults;
        public Queue<Value> processedFunctionResults;

        public FunctionFrame(string identifier) : base()
        {
            this.Identifier = identifier;
            this.functionResults = new Queue<Value>();
            this.processedFunctionResults = new Queue<Value>();
            LocalVariables = new Dictionary<string, Value>();
            ExpectedArguments = 0;
            ReturnResult = Value.Null;
        }

        public FunctionFrame CloneTemplate()
        {
            FunctionFrame functionStructure = new FunctionFrame(Identifier);
            functionStructure.SetArgumentParameters(argument_identifiers);
            functionStructure.SetArguments(LocalVariables.Values.ToList());
            return functionStructure;
        }

        public void SetArgumentParameters(List<string> argument_identifiers)
        {
            this.argument_identifiers = argument_identifiers;
            ExpectedArguments = argument_identifiers.Count;
            for (int i = 0; i < argument_identifiers.Count; i++)
            {
                LocalVariables.Add(argument_identifiers[i], Value.Null);
            }
        }

        public void SetArguments(List<Value> arguments)
        {
            if(ExpectedArguments != arguments.Count)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            for (int i = 0; i < ExpectedArguments; i++)
            {
                LocalVariables[argument_identifiers[i]] = arguments[i];
            }
        }
    }

    class IfElifFrame:CallFrame
    {
        public bool Result { get; set; }

        public IfElifFrame():base()
        {
            Result = false;
        }
    }

    class ElseFrame : CallFrame
    {
        public ElseFrame():base()
        {

        }
    }

    class CallFrame
    {
        public Marker StartPosition { get; set; }

        public CallFrame()
        {

        }
    }
}
