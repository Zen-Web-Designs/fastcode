﻿using fastcode.parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fastcode.runtime
{
    enum ControlStructureType
    {
        MainProgram,
        If,
        Elif,
        Else,
        Count,
        While,
        Function,
        Forever //sometimes you have these, while trues that can just be implemented as forever. Also, this is the first repeating control structure implemented -wanna test it out first.
    }

    enum ControlStructureRepeatStatus
    {
        Continue, //continues executing the body
        Return, //skips
    }

    class CountStructure : ControlStructure
    {
        public int CountTo { get; set; }
        public int Count { get; set; }
        public int CountFrom { get; set; }
        public string IndexerIdentifier { get; set; }

        public CountStructure() : base(ControlStructureType.Count)
        {

        }
    }

    class WhileStructure : ControlStructure
    {
        public Marker ExpressionMarker { get; set; } //for control structures set to evaluate

        public WhileStructure() : base(ControlStructureType.While)
        {

        }
    }

    class FunctionStructure : ControlStructure
    {
        public string Identifier { get; private set; }
        List<string> argument_identifiers;
        public Dictionary<string, Value> Arguments { get; private set; }
        public int ExpectedArguments { get; private set; }
        public bool FinishedExecuting { get; private set; }
        public Marker ReturnPosition { get; private set; }

        public FunctionStructure(string identifier) : base(ControlStructureType.Function)
        {
            this.Identifier = identifier;
            Arguments = new Dictionary<string, Value>();
            ExpectedArguments = 0;
            RepeatStatus = ControlStructureRepeatStatus.Return;
            MarkAsExecuting();
        }

        public FunctionStructure Clone()
        {
            FunctionStructure functionStructure = new FunctionStructure(Identifier);
            functionStructure.SetArgumentParameters(argument_identifiers);
            functionStructure.SetArguments(Arguments.Values.ToList());
            if(FinishedExecuting)
            {
                functionStructure.MarkAsFinished();
            }
            else
            {
                functionStructure.MarkAsExecuting();
            }
            functionStructure.MarkReturnPosition(ReturnPosition);
            return functionStructure;
        }

        public void SetArgumentParameters(List<string> argument_identifiers)
        {
            this.argument_identifiers = argument_identifiers;
            ExpectedArguments = argument_identifiers.Count;
            for (int i = 0; i < argument_identifiers.Count; i++)
            {
                Arguments.Add(argument_identifiers[i], Value.Null);
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
                Arguments[argument_identifiers[i]] = arguments[i];
            }
        }

        public void Reset()
        {
            StartPosition = null;
            FinishedExecuting = false;
            Result = null;
            Arguments.Clear();
            for (int i = 0; i < ExpectedArguments; i++)
            {
                Arguments.Add(argument_identifiers[i], Value.Null);
            }
        }

        public void MarkAsExecuting()
        {
            FinishedExecuting = false;
        }

        public void MarkAsFinished()
        {
            FinishedExecuting = true;
        }

        public void MarkReturnPosition(Marker ret)
        {
            this.ReturnPosition = ret;
        }
    }

    class ControlStructure
    {
        public HashSet<int> functionEvaluativeLocations;
        public HashSet<int> expressionStartLocations;
        public ControlStructureRepeatStatus RepeatStatus { get; set; }
        public ControlStructureType Type { get; private set; }
        public object Result { get; set; }
        public Marker StartPosition { get; set; }


        public ControlStructure(ControlStructureType type)
        {
            this.Type = type;
            this.RepeatStatus = ControlStructureRepeatStatus.Continue;
            this.functionEvaluativeLocations = new HashSet<int>();
            this.expressionStartLocations = new HashSet<int>();
        }
    }
}