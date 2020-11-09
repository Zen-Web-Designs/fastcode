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
        If,
        Elif,
        Else,
        Forloop,
        While,
        Function
    }

    class ForStructure : ControlStructure
    {
        public int currentIndex { get; set; }
        public List<Value> Values { get; set; }
        public string IndexerIdentifier { get; set; }

        public ForStructure() : base(ControlStructureType.Forloop)
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
        public Dictionary<string, Value> LocalVariables { get; private set; }
        public int ExpectedArguments { get; private set; }
        public bool FinishedExecuting { get; private set; }
        public Marker ReturnPosition { get; private set; }

        public Queue<Value> functionResults;
        public Queue<Value> processedFunctionResults;

        public FunctionStructure(string identifier) : base(ControlStructureType.Function)
        {
            this.Identifier = identifier;
            this.functionResults = new Queue<Value>();
            this.processedFunctionResults = new Queue<Value>();
            LocalVariables = new Dictionary<string, Value>();
            ExpectedArguments = 0;
            WillRepeat = false;
            MarkAsExecuting();
        }

        public FunctionStructure Clone()
        {
            FunctionStructure functionStructure = new FunctionStructure(Identifier);
            functionStructure.SetArgumentParameters(argument_identifiers);
            functionStructure.SetArguments(LocalVariables.Values.ToList());
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

        public void Reset()
        {
            StartPosition = null;
            FinishedExecuting = false;
            Result = null;
            LocalVariables.Clear();
            for (int i = 0; i < ExpectedArguments; i++)
            {
                LocalVariables.Add(argument_identifiers[i], Value.Null);
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
        public bool WillRepeat { get; set; }
        public ControlStructureType Type { get; private set; }
        public object Result { get; set; }
        public Marker StartPosition { get; set; }


        public ControlStructure(ControlStructureType type)
        {
            this.Type = type;
            this.WillRepeat = true;
        }
    }
}
