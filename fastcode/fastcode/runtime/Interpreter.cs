﻿using fastcode.flib;
using fastcode.parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fastcode.runtime
{
    public class Interpreter
    {
        //IO usually redirected to console, but could be changed
        public TextWriter Output { get; private set; } 
        public TextReader Input { get; private set; }
        
        public Marker Position
        {
            get { return lexer.Position; }
        }

        Dictionary<string, Library> BuiltInLibraries = new Dictionary<string, Library>()
        {
            {"flib.stdlib", new StandardLibrary() }
        };

        public Dictionary<string, Value> GlobalVariables { get; private set; } //dictionaries are used for fast access

        public delegate Value BuiltInFunction(List<Value> arguments);
        Dictionary<string, BuiltInFunction> builtInFunctions;
        Dictionary<string, FunctionStructure> functions;

        Lexer lexer; //parsing aides
        Token prevToken;
        Token lastToken;
        int bracket_counter; //counts how "deep" you go. Not meant as a dirty joke 
        private Marker expressionMarker; //counts the start of the expresion
        private Marker keywordMarker; //start of keyword

        //this makes up our "call stack" for control structures. Also includes whiles and elses and that stuff rather than functions
        Stack<ControlStructure> CallStack;
        ControlStructure prevStructure;

        List<Tuple<int, List<Value>>> functionEvaluativeStack; //used when evaluating expressions that call functions
        List<List<Value>> argumentEvaluativeQueue;
        Stack<int> argumentEvaluations;
        Stack<int> functionEvaluations;
        int currentEvalArgument;
        int currentFunction;

        public bool Exit { get; private set; } //exit condition - private set so the program cannot be aborted from the outside without going through an exit function

        private string[] ReadOnlyVariables = { "null", "true", "false", "endl"};

        public Interpreter(TextWriter output, TextReader input, string source)
        { 
            //initialize a bunch of crap
            this.Output = output;
            this.Input = input;
            this.GlobalVariables = new Dictionary<string, Value>();
            this.builtInFunctions = new Dictionary<string, BuiltInFunction>();
            this.functions = new Dictionary<string, FunctionStructure>();
            this.CallStack = new Stack<ControlStructure>();
            this.functionEvaluativeStack = new List<Tuple<int, List<Value>>>();
            this.argumentEvaluativeQueue = new List<List<Value>>();
            this.argumentEvaluations = new Stack<int>();
            this.functionEvaluations = new Stack<int>();
            GlobalVariables["null"] = Value.Null;
            GlobalVariables["true"] = new Value(1);
            GlobalVariables["false"] = new Value(0);
            GlobalVariables["endl"] = new Value(Environment.NewLine);
            lexer = new Lexer(source);
            bracket_counter = 0;
            currentEvalArgument = 0;
            currentFunction = -1;
        }

        //starts the program
        public void Start()
        {
            builtInFunctions.Clear();
            CallStack.Clear();
            functionEvaluativeStack.Clear();
            CallStack.Push(new FunctionStructure("MAINSTRUCTURE"));
            BuiltInLibraries["flib.stdlib"].Install(ref builtInFunctions, this);
            keywordMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
            ReadNextToken();
            while(Exit == false) //program loop
            {
                while(lastToken == Token.Newline)
                {
                    ReadNextToken();
                }
                if(lastToken == Token.EndOfFile || CallStack.Count == 0)
                {
                    Exit = true;
                    break;
                }
                ExecuteNextStatement(); //execute a statement
                keywordMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);

                //expect an EOF or \n at the end of a line
                if (lastToken != Token.Newline && lastToken != Token.EndOfFile)
                {
                    //error handling
                    throw new UnexpectedStatementException("a newline or EOF", lastToken.ToString());
                }
            }
        }

        //checks to see if the token is the same as the interpreter expected
        void MatchToken(Token token)
        {
            if(token != lastToken)
            {
                throw new UnexpectedStatementException(token.ToString(), lastToken.ToString());
            }
        }

        //executes a single statement
        void ExecuteNextStatement()
        {
            Token keyword = lastToken;
            expressionMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
            ReadNextToken();
            switch (keyword)
            {
                case Token.Identifier:
                    string id = lexer.TokenIdentifier;
                    if (lastToken == Token.Set)
                    {
                        if(ReadOnlyVariables.Contains(id))
                        {
                            throw new Exception("FastCode cannot write to a read only variable.");
                        }
                        ReadNextToken();
                        Value v1 = EvaluateNextExpression();
                        if(v1 == null)
                        {
                            return;
                        }
                        if(GlobalVariables.ContainsKey(id))
                        {
                            GlobalVariables[id] = v1;
                            break;
                        }
                        else
                        {
                            Stack<ControlStructure> searched = new Stack<ControlStructure>();
                            while (CallStack.Count != 0)
                            {
                                ControlStructure controlStructure = CallStack.Pop();
                                if (controlStructure.Type == ControlStructureType.Function)
                                {
                                    FunctionStructure f = (FunctionStructure)controlStructure;
                                    if(f.LocalVariables.ContainsKey(id))
                                    {
                                        f.LocalVariables[id] = v1;
                                    }
                                    else if(f.Identifier != "MAINSTRUCTURE")
                                    {
                                        f.LocalVariables.Add(id, v1);
                                    }
                                    else
                                    {
                                        GlobalVariables.Add(id, v1);
                                    }
                                    searched.Push(controlStructure);
                                    break;
                                }
                                searched.Push(controlStructure);
                            }
                            while (searched.Count != 0)
                            {
                                CallStack.Push(searched.Pop());
                            }
                        }
                        break;
                    }
                    else if(lastToken == Token.OpenParenthesis)
                    {
                        if(functions.ContainsKey(id) ||builtInFunctions.ContainsKey(id))
                        {
                            lexer.ShiftCurrentPosition(keywordMarker);
                            ReadNextToken();
                            var n = EvaluateNextExpression();
                            return;
                        }
                    }
                    else if(lastToken == Token.OpenBracket)
                    {
                        ReadNextToken();
                        Value v = EvaluateNextExpression();
                        if (v == null)
                        {
                            return;
                        }
                        if (v.Type != ValueType.Double)
                        {
                            throw new Exception("Indicie's must be of type double.");
                        }
                        MatchToken(Token.CloseBracket);
                        ReadNextToken();
                        MatchToken(Token.Set);
                        ReadNextToken();
                        Value setval = EvaluateNextExpression();
                        if(setval == null)
                        {
                            return;
                        }
                        if (GlobalVariables.ContainsKey(id))
                        {
                            if (GlobalVariables[id].Type == ValueType.Array)
                            {
                                GlobalVariables[id].Array[(int)v.Double] = setval;
                            }
                            else if (GlobalVariables[id].Type == ValueType.String)
                            {
                                if (setval.Type != ValueType.Char)
                                {
                                    throw new Exception("Strings can only index characters.");
                                }
                                char[] str = GlobalVariables[id].String.ToCharArray();
                                str[(int)v.Double] = setval.Character;
                                GlobalVariables[id] = new Value(new string(str));
                            }
                        }
                        else
                        {
                            bool flag = false;
                            Stack<ControlStructure> searched = new Stack<ControlStructure>();
                            while (CallStack.Count != 0)
                            {
                                ControlStructure controlStructure = CallStack.Pop();
                                if (controlStructure.Type == ControlStructureType.Function)
                                {
                                    FunctionStructure f = (FunctionStructure)controlStructure;
                                    if (f.LocalVariables[id].Type == ValueType.Array)
                                    {
                                        f.LocalVariables[id].Array[(int)v.Double] = setval;
                                        flag = true;
                                    }
                                    else if (f.LocalVariables[id].Type == ValueType.String)
                                    {
                                        if (setval.Type != ValueType.Char)
                                        {
                                            throw new Exception("Strings can only index characters.");
                                        }
                                        char[] str = f.LocalVariables[id].String.ToCharArray();
                                        str[(int)v.Double] = setval.Character;
                                        f.LocalVariables[id] = new Value(new string(str));
                                        flag = true;
                                    }
                                    searched.Push(controlStructure);
                                    break;
                                }
                                searched.Push(controlStructure);
                            }
                            while (searched.Count != 0)
                            {
                                CallStack.Push(searched.Pop());
                            }
                            if(flag)
                            {
                                break;
                            }
                        }
                    }
                    throw new Exception("Identifiers cannot stand alone without a keyword.");
                case Token.Assert:
                    MatchToken(Token.OpenParenthesis);
                    Value expr = EvaluateNextExpression();
                    if(expr == null)
                    {
                        return;
                    }
                    if (expr.PerformBinaryOperation(Token.Equals,new Value(0)).Double == 1)
                    {
                        throw new AssertionFailedException();
                    }
                    break;
                case Token.Break:
                    if(CallStack.Peek().Type == ControlStructureType.Function)
                    {
                        throw new UnexpectedKeyword(Token.Break);
                    }
                    else
                    {
                        int i = 0;
                        while (!(CallStack.Peek().Type == ControlStructureType.While || CallStack.Peek().Type == ControlStructureType.Forever || CallStack.Peek().Type == ControlStructureType.Count))
                        {
                            if (CallStack.Peek().Type == ControlStructureType.Function)
                            {
                                throw new UnexpectedKeyword(Token.Break);
                            }
                            i++;
                            prevStructure = CallStack.Pop();
                        }
                        ControlStructure current66 = CallStack.Pop();
                        current66.RepeatStatus = ControlStructureRepeatStatus.Return;
                        prevStructure = current66;
                        SkipControlStructure(-i);
                        
                        break;
                    }
                case Token.Return:
                    FunctionStructure function = null;
                    int j = 0;
                    while (CallStack.Count != 0)
                    {
                        ControlStructure structure = CallStack.Pop();
                        if(structure.Type == ControlStructureType.Function)
                        {
                            function = (FunctionStructure)structure;
                            break;
                        }
                        j++;
                    }
                    if(function == null)
                    {
                        throw new Exception("Only functions may return values.");
                    }

                    if (PeekNextToken() != Token.Newline && PeekNextToken() != Token.Semicolon)
                    {
                        CallStack.Push(function);
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        else
                        {
                            ;
                        }
                        function = (FunctionStructure)CallStack.Pop();
                        function.Result = expr;
                    }
                    CallStack.Push(function);
                    SkipControlStructure(-j,true);
                     ExecuteNextStatement();

                    break;
                case Token.Stop:
                    Exit = true;
                    return;
                case Token.Import:
                    MatchToken(Token.Value);
                    if(lexer.TokenValue.Type != ValueType.String)
                    {
                        throw new Exception("Expected string, got " + lexer.TokenValue.Type);
                    }
                    if(BuiltInLibraries.ContainsKey(lexer.TokenValue.String))
                    {
                        BuiltInLibraries[lexer.TokenValue.String].Install(ref builtInFunctions, this);
                    }
                    else
                    {
                        throw new Exception("Cannot find library " + lexer.TokenValue.Type);
                    }
                    ReadNextToken();
                    break;
                case Token.EndOfFile:
                    Exit = true;
                    return;
                case Token.If:
                    ControlStructure current = new ControlStructure(ControlStructureType.If);
                    Value expr1 = EvaluateNextExpression();
                    if(expr1 == null)
                    {
                        return;
                    }
                    else
                    current.Result = (expr1.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1); //not not the actual result, it just checks if the condition failed so it can skip that section. Kinda misleading if you didn't know - just refer to the assertion token's case.
                    CallStack.Push(current);

                    ReadTillControlStructureStart(false); //read till open bracket

                    if((bool)current.Result == true) //skip till close bracket.
                    {
                        SkipControlStructure();
                        prevStructure = CallStack.Pop();
                    }
                    current.RepeatStatus = ControlStructureRepeatStatus.Return; //sets to return, ifs never repeat
                    
                    break;
                case Token.Else: //not if results are inverted 
                    if (prevStructure.Type != ControlStructureType.If && prevStructure.Type != ControlStructureType.Elif)
                    {
                        throw new UnexpectedKeyword(keyword);
                    }
                    else if ((bool)prevStructure.Result == true) //skip all the crap
                    {
                        CallStack.Push(new ControlStructure(ControlStructureType.Else));
                        ReadTillControlStructureStart(false);
                        ControlStructure current2 = CallStack.Pop();
                        current2.Result = null;
                        current2.RepeatStatus = ControlStructureRepeatStatus.Return;
                        CallStack.Push(current2);
                    }
                    else if ((bool)prevStructure.Result == false)
                    {
                        CallStack.Push(new ControlStructure(ControlStructureType.Else));
                        ReadTillControlStructureStart(false);
                        SkipControlStructure();
                        ControlStructure current3 = CallStack.Pop();
                        current3.Result = null;
                        current3.RepeatStatus = ControlStructureRepeatStatus.Return;
                        prevStructure = current3;
                    }
                    break;
                case Token.Elif:
                    if(prevStructure.Type != ControlStructureType.If && prevStructure.Type != ControlStructureType.Elif)
                    {
                        throw new UnexpectedKeyword(keyword);
                    }
                    else
                    {
                        ControlStructure current5 = new ControlStructure(ControlStructureType.Elif);
                        current5.RepeatStatus = ControlStructureRepeatStatus.Return;
                        CallStack.Push(current5);
                        current5.Result = false;
                        if ((bool)prevStructure.Result == true)
                        {
                            ControlStructure control4 = CallStack.Pop();
                            expr = EvaluateNextExpression();
                            if(expr == null)
                            {
                                return;
                            }
                            control4.Result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1);
                            CallStack.Push(control4);
                            ReadTillControlStructureStart(false);
                            if ((bool)CallStack.Peek().Result == true)
                            {
                                SkipControlStructure();
                                prevStructure = CallStack.Pop();
                            }
                        }
                        else
                        {
                            ReadTillControlStructureStart(false);
                            SkipControlStructure();
                            prevStructure = CallStack.Pop();
                        }
                    }
                    break;
                case Token.Forever:
                    CallStack.Push(new ControlStructure(ControlStructureType.Forever));
                    ReadTillControlStructureStart();
                    break;
                case Token.While:
                    WhileStructure whileStructure = new WhileStructure();
                    whileStructure.ExpressionMarker = new Marker(expressionMarker.Index, expressionMarker.Collumn, expressionMarker.Row);
                    expr = EvaluateNextExpression();
                    if(expr == null)
                    {
                        return;
                    }
                    whileStructure.Result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1);
                    CallStack.Push(whileStructure);
                    ReadTillControlStructureStart();
                    if ((bool)whileStructure.Result == true)
                    {
                        whileStructure.RepeatStatus = ControlStructureRepeatStatus.Return;
                        SkipControlStructure();
                        prevStructure = CallStack.Pop();
                    }
                    break;
                case Token.For:
                    ForStructure forStructure = new ForStructure();
                    MatchToken(Token.Identifier);
                    forStructure.IndexerIdentifier = lexer.TokenIdentifier;
                    ReadNextToken();
                    MatchToken(Token.In);
                    ReadNextToken();
                    expr = EvaluateNextExpression();
                    if(expr == null)
                    {
                        return;
                    }
                    if(expr.Type == ValueType.Array)
                    {
                        forStructure.Values = expr.Array;
                    }
                    else if(expr.Type == ValueType.String)
                    {
                        forStructure.Values = new List<Value>();
                        for (int i = 0; i < expr.String.Length; i++)
                        {
                            forStructure.Values.Add(new Value(expr.String[i]));
                        }
                    }
                    else
                    {
                        throw new Exception("Fastcode can only iterate through an array or string.");
                    }
                    forStructure.currentIndex = 0;
                    Stack<ControlStructure> searched2 = new Stack<ControlStructure>();
                    while(CallStack.Count > 0)
                    {
                        searched2.Push(CallStack.Pop());
                        if(searched2.Peek().GetType() == typeof(FunctionStructure))
                        {
                            FunctionStructure functionStructure1 = (FunctionStructure)searched2.Peek();
                            if (forStructure.Values.Count > 0)
                            {
                                if (functionStructure1.LocalVariables.ContainsKey(forStructure.IndexerIdentifier))
                                {
                                    functionStructure1.LocalVariables[forStructure.IndexerIdentifier] = forStructure.Values[0];
                                }
                                else
                                {
                                    functionStructure1.LocalVariables.Add(forStructure.IndexerIdentifier, forStructure.Values[0]);
                                }
                            }
                            break;
                        }
                    }
                    while(searched2.Count > 0)
                    {
                        CallStack.Push(searched2.Pop());
                    }

                    CallStack.Push(forStructure);
                    ReadTillControlStructureStart();
                    if(forStructure.currentIndex >= forStructure.Values.Count)
                    {
                        SkipControlStructure();
                        forStructure.RepeatStatus = ControlStructureRepeatStatus.Return;
                        prevStructure = CallStack.Pop();
                    }
                    break;
                case Token.Function:
                    MatchToken(Token.Identifier);
                    string fid = lexer.TokenIdentifier;
                    if(functions.ContainsKey(fid) || GlobalVariables.ContainsKey(fid) || builtInFunctions.ContainsKey(fid))
                    {
                        throw new Exception("Identifiers must be unique");
                    }
                    FunctionStructure functionStructure = new FunctionStructure(fid);
                    ReadNextToken();
                    MatchToken(Token.OpenParenthesis);
                    List<string> argument_identifiers = new List<string>();
                    while(lastToken != Token.CloseParenthesis)
                    {
                        ReadNextToken();
                        if(lastToken == Token.Comma || lastToken == Token.CloseParenthesis)
                        {
                            continue;
                        }
                        else if(lastToken == Token.Identifier)
                        {
                            if(argument_identifiers.Contains(lexer.TokenIdentifier) || GlobalVariables.ContainsKey(lexer.TokenIdentifier))
                            {
                                throw new Exception("Argument identifiers must be unique.");
                            }
                            argument_identifiers.Add(lexer.TokenIdentifier);
                        }
                        else if(lastToken == Token.CloseParenthesis)
                        {
                            break;
                        }
                        else
                        {
                            throw new UnexpectedStatementException("an identifier", lastToken.ToString());
                        }
                    }
                    functionStructure.SetArgumentParameters(argument_identifiers);
                    CallStack.Push(functionStructure);
                    ReadTillControlStructureStart();
                    functionStructure = (FunctionStructure)CallStack.Pop();
                    functions[fid] = functionStructure;
                    SkipControlStructure();
                    break;
                case Token.CloseBrace: //checks to return or repeat. 
                    bracket_counter--;
                    if(CallStack.Peek().GetType() == typeof(WhileStructure))
                    {
                        Marker currentpos = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
                        lexer.ShiftCurrentPosition(((WhileStructure)CallStack.Peek()).ExpressionMarker);
                        ReadNextToken();
                        expr = EvaluateNextExpression();
                        if (expr == null)
                        {
                            return;
                        }
                        bool result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1);
                        if(!result)
                        {
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                        else
                        {
                            try
                            {
                                lexer.ShiftCurrentPosition(currentpos);
                                ReadNextToken();
                            }
                            catch
                            {
                                Exit = true;
                                return;
                            }
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(ForStructure))
                    {
                        ForStructure forStructure2 = (ForStructure)CallStack.Pop();
                        forStructure2.RepeatStatus = ControlStructureRepeatStatus.Return;
                        forStructure2.currentIndex++;
                        FunctionStructure functionStructure2 = null;
                        Stack<ControlStructure> searched3 = new Stack<ControlStructure>();
                        while (CallStack.Count > 0)
                        {
                            searched3.Push(CallStack.Pop());
                            if (searched3.Peek().GetType() == typeof(FunctionStructure))
                            {
                                functionStructure2 = (FunctionStructure)searched3.Peek();
                            }
                        }
                        if (forStructure2.currentIndex < forStructure2.Values.Count)
                        {
                            functionStructure2.LocalVariables[forStructure2.IndexerIdentifier] = forStructure2.Values[forStructure2.currentIndex];
                            forStructure2.RepeatStatus = ControlStructureRepeatStatus.Continue;
                            while (searched3.Count > 0)
                            {
                                CallStack.Push(searched3.Pop());
                            }
                            CallStack.Push(forStructure2);
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                        else
                        {
                            functionStructure2.LocalVariables.Remove(forStructure2.IndexerIdentifier);
                            while (searched3.Count > 0)
                            {
                                CallStack.Push(searched3.Pop());
                            }
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(FunctionStructure))
                    {
                        FunctionStructure finishedfunction = (FunctionStructure)CallStack.Pop();
                        finishedfunction.MarkAsFinished();
                        prevStructure = finishedfunction;
                        lexer.ShiftCurrentPosition(finishedfunction.ReturnPosition);
                        //Tuple<int, List<Value>> currentEval = functionEvaluativeStack.Pop();
                        //currentEval.Item2.Add((Value)finishedfunction.Result);
                        //functionEvaluativeStack.Push(currentEval);
                        functionEvaluativeStack[functionEvaluativeStack.Count-1].Item2.Add((Value)finishedfunction.Result);
                    }
                    else if (CallStack.Peek().RepeatStatus == ControlStructureRepeatStatus.Return)
                    {
                        prevStructure = CallStack.Pop();
                    }
                    else if (CallStack.Peek().RepeatStatus == ControlStructureRepeatStatus.Continue)
                    {
                        lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                        ReadNextToken();
                    }
                    break;
                default:
                    throw new UnexpectedKeyword(keyword);
            
            }
            if(lastToken == Token.Semicolon)
            {
                keywordMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
                ReadNextToken();
                while (lastToken == Token.Newline)
                {
                    ReadNextToken();
                }
                ExecuteNextStatement();
            }
        }

        //reads the next token
        Token ReadNextToken()
        {
            prevToken = lastToken;
            lastToken = lexer.ReadNextToken();
            return lastToken;
        }

        //Reads the next non-newline token
        Token PeekNextToken()
        {
            Marker marker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
            Token old = lastToken;
            ReadNextToken();
            while(lastToken == Token.Newline)
            {
                ReadNextToken();
            }
            Token tok = lastToken;
            lastToken = old;
            lexer.ShiftCurrentPosition(marker);
            return tok;
        }

        public void ReadTillControlStructureStart(bool markposition=true)
        {
            while (lastToken != Token.OpenBrace)
            {
                ReadNextToken();
            }
            bracket_counter++; //see one bracket;
            ReadNextToken();

            if(markposition)
            {
                ControlStructure current = CallStack.Pop();
                current.StartPosition = new Marker(lexer.Position.Index - 1, lexer.Position.Collumn, lexer.Position.Row);
                CallStack.Push(current);
            }
        }

        public void SkipControlStructure(int offset = 0, bool readtok = false)
        {
            int i = bracket_counter - 1 + offset;

            while (i != bracket_counter)
            {
                if (lastToken == Token.OpenBrace)
                {
                    bracket_counter++;
                }
                else if (lastToken == Token.CloseBrace)
                {
                    bracket_counter--;
                }
                if(readtok)
                {
                    if(i == bracket_counter)
                    {
                        break;
                    }
                }
                ReadNextToken();
            }
        }

        Value EvaluateNextExpression(bool keepargcount = false)
        {
            int lid = lexer.Position.Index;
            currentFunction++;

            FunctionStructure currentStructure = null;
            Stack<ControlStructure> searched = new Stack<ControlStructure>();
            while(CallStack.Count > 0)
            {
                searched.Push(CallStack.Pop());
                if(searched.Peek().GetType() == typeof(FunctionStructure))
                {
                    currentStructure = (FunctionStructure)searched.Peek();
                    break;
                }
            }
            while(searched.Count > 0)
            {
                CallStack.Push(searched.Pop());
            }

            if (!currentStructure.functionEvaluativeLocations.Contains(lid))
            {
                currentStructure.functionEvaluativeLocations.Add(lid);
                functionEvaluativeStack.Add(new Tuple<int, List<Value>>(0, new List<Value>()));
            }
            if (!keepargcount)
            {
                if (!currentStructure.expressionStartLocations.Contains(lid))
                {
                    currentStructure.expressionStartLocations.Add(lid);
                    argumentEvaluations.Push(argumentEvaluativeQueue.Count);
                    functionEvaluations.Push(currentFunction);
                }
                currentEvalArgument = argumentEvaluations.Peek();
                currentFunction = functionEvaluations.Peek();
            }
            Value val = EvaluateNextExpression(0);
            if(val == null)
            {
                if (!keepargcount)
                {
                    if (!currentStructure.expressionStartLocations.Contains(lid))
                    {
                        currentStructure.expressionStartLocations.Add(lid);
                        argumentEvaluations.Push(currentEvalArgument);
                        functionEvaluations.Push(currentFunction);
                    }
                }
                return null;
            }
            else
            {
                if (!keepargcount)
                {
                    currentStructure.expressionStartLocations.Remove(lid);
                    functionEvaluations.Pop();
                    argumentEvaluations.Pop();
                }
                functionEvaluativeStack.RemoveAt(functionEvaluativeStack.Count - 1);
                currentStructure.functionEvaluativeLocations.Remove(lid);
                currentFunction--;
                return val;
            }
        }

        //this and next value are really important because that's how values for arguments are ascertained
        //gets the next expression (conditions, expressions) and evaluates it. Return's 0 or 1 for conditions
        Value EvaluateNextExpression(int min)
        {
            Dictionary<Token, int> precedens = new Dictionary<Token, int>()
            {
                { Token.Or, 0 }, { Token.And, 0 },
                { Token.Equals, 1 }, { Token.NotEqual, 1 },
                { Token.Less, 1 }, { Token.More, 1 },
                { Token.LessEqual, 1 },  { Token.MoreEqual, 1 },
                { Token.Plus, 2 }, { Token.Minus, 2 },
                { Token.Asterisk, 3 }, {Token.Modulous, 3 }, {Token.Slash, 3 },
                { Token.Caret, 4 }
            }; //counts the amount of required "arguments" for each operand
            Value value = NextValue();
            if(value == null)
            {
                return null;
            }
            while (true)
            {
                //we could just put precedens.contains token but you know.... 
                if (lastToken < Token.Plus || lastToken > Token.And || precedens[lastToken] < min)
                    break;

                Token op = lastToken;
                int prec = precedens[lastToken]; // Operator Precedence
                int assoc = 0; // 0 left, 1 right; Operator associativity
                int nextmin = assoc == 0 ? prec : prec + 1;
                ReadNextToken();
                Value rhs = EvaluateNextExpression(nextmin);
                if(rhs == null)
                {
                    return null;
                }
                value = value.PerformBinaryOperation(op, rhs);
            }

            return value;
        }

        //gets the next value
        Value NextValue()
        {
            Value val = Value.Null;
            if(lastToken == Token.Value) //raw value
            {
                val = lexer.TokenValue;
                ReadNextToken();
            }
            else if(lastToken == Token.Identifier)
            {
                if(GlobalVariables.ContainsKey(lexer.TokenIdentifier)) //see if it's a variable
                {
                    string vid = lexer.TokenIdentifier;
                    if (PeekNextToken() == Token.OpenBracket)
                    {
                        ReadNextToken();
                        ReadNextToken();
                        Value v = EvaluateNextExpression();
                        if (v == null)
                        {
                            return null;
                        }
                        if (v.Type != ValueType.Double)
                        {
                            throw new Exception("Indicie's must be of type double.");
                        }
                        if (GlobalVariables[vid].Type == ValueType.String)
                        {
                            val = new Value(GlobalVariables[vid].String[(int)v.Double]);
                        }
                        else if (GlobalVariables[vid].Type == ValueType.Array)
                        {
                            val = GlobalVariables[vid].Array[(int)v.Double];
                        }
                        else
                        {
                            throw new Exception("Only arrays and strings can be indexed.");
                        }
                        MatchToken(Token.CloseBracket);
                    }
                    else
                    {
                        val = GlobalVariables[lexer.TokenIdentifier];
                    }
                }
                else if(functions.ContainsKey(lexer.TokenIdentifier) || builtInFunctions.ContainsKey(lexer.TokenIdentifier)) //see if it's a function
                {
                    ReadNextToken();
                    MatchToken(Token.OpenParenthesis);
                    string fid = lexer.TokenIdentifier;
                    int aid = lexer.Position.Index;
                    List<Value> arguments = new List<Value>();
                    if(currentEvalArgument>=argumentEvaluativeQueue.Count)
                    {
                        argumentEvaluativeQueue.Add(arguments);
                    }
                    else
                    {
                        arguments = argumentEvaluativeQueue[currentEvalArgument];
                    }
                    currentEvalArgument++;
                    int argno = 0; //current argument index
                    while (lastToken != Token.CloseParenthesis) //collect all the arguments
                    {
                        ReadNextToken();
                        if(lastToken == Token.Comma || lastToken == Token.CloseParenthesis)
                        {
                            continue; //just skip the comma's
                        }
                        else
                        {
                            if (argno >= arguments.Count)
                            {
                                Value v = EvaluateNextExpression(true);
                                if (v == null)
                                {
                                    return null; //this is how to escape the recursive function when a function needs to be evaluated through the main loop first
                                }
                                arguments.Add(v);
                            }
                            else
                            {
                                int paramCount = 0; //skip open params, close params till it balances out
                                do
                                {
                                    ReadNextToken();
                                    if (lastToken == Token.OpenParenthesis)
                                    {
                                        paramCount++;
                                    }
                                    else if (lastToken == Token.CloseParenthesis)
                                    {
                                        paramCount--;
                                    }
                                }
                                while (paramCount >= 0 && lastToken!= Token.Comma);
                            }
                        }
                        argno++;
                    }
                    if (functions.ContainsKey(fid))
                    {
                        if (functionEvaluativeStack[currentFunction].Item1 < functionEvaluativeStack[currentFunction].Item2.Count)
                        {
                            argumentEvaluativeQueue.RemoveAt(argumentEvaluativeQueue.Count-1);
                            Value value = functionEvaluativeStack[currentFunction].Item2[functionEvaluativeStack[currentFunction].Item1];
                            Tuple<int, List<Value>> newEval2 = new Tuple<int, List<Value>>(functionEvaluativeStack[currentFunction].Item1 + 1, functionEvaluativeStack[currentFunction].Item2);
                            functionEvaluativeStack[currentFunction] = newEval2;
                            ReadNextToken();
                            return value;
                        }
                        //val = functions[lexer.TokenIdentifier](this, arguments); //evaluate the value
                        FunctionStructure f = functions[fid].Clone();
                        f.MarkAsExecuting();
                        f.SetArguments(arguments);
                        f.MarkReturnPosition(keywordMarker);
                        f.Result = Value.Null;
                        CallStack.Push(f);
                        lexer.ShiftCurrentPosition(functions[fid].StartPosition);
                        ReadNextToken();
                        Tuple<int, List<Value>> newEval = new Tuple<int, List<Value>>(0, functionEvaluativeStack[currentFunction].Item2);
                        functionEvaluativeStack[currentFunction] = newEval;
                        return null;
                    }
                    else
                    {
                        argumentEvaluativeQueue.RemoveAt(argumentEvaluativeQueue.Count-1);
                        ReadNextToken();
                        return builtInFunctions[fid].Invoke(arguments);
                    }
                }
                else
                {
                    val = null;
                    Stack<ControlStructure> searched = new Stack<ControlStructure>();
                    while(CallStack.Count != 0)
                    {
                        ControlStructure controlStructure = CallStack.Pop();
                        if(controlStructure.Type == ControlStructureType.Function)
                        {
                            FunctionStructure function = (FunctionStructure)controlStructure;
                            if(function.LocalVariables.ContainsKey(lexer.TokenIdentifier))
                            {
                                string fid = lexer.TokenIdentifier;
                                if (PeekNextToken() == Token.OpenBracket)
                                {
                                    ReadNextToken();
                                    ReadNextToken();
                                    Value v = EvaluateNextExpression();
                                    if (v == null)
                                    {
                                        return null;
                                    }
                                    if (v.Type != ValueType.Double)
                                    {
                                        throw new Exception("Indicie's must be of type double.");
                                    }
                                    if (function.LocalVariables[fid].Type == ValueType.String)
                                    {
                                        val = new Value(function.LocalVariables[fid].String[(int)v.Double]);
                                    }
                                    else if (function.LocalVariables[fid].Type == ValueType.Array)
                                    {
                                        val = function.LocalVariables[fid].Array[(int)v.Double];
                                    }
                                    else
                                    {
                                        throw new Exception("Only arrays and strings can be indexed.");
                                    }
                                    MatchToken(Token.CloseBracket);
                                }
                                else
                                {
                                    val = function.LocalVariables[fid];
                                }
                                searched.Push(function);
                                break;
                            }
                        }
                        searched.Push(controlStructure);
                    }
                    while(searched.Count != 0)
                    {
                        CallStack.Push(searched.Pop());
                    }
                    if (val == null)
                    {
                        throw new UnidentifiedTokenExcepion();
                    }
                }
                ReadNextToken();
            }
            else if(lastToken == Token.OpenParenthesis) //it's an expression
            {
                ReadNextToken();
                val = EvaluateNextExpression(); //call the evaluate function
                if(val == null)
                {
                    return null;
                }
                MatchToken(Token.CloseParenthesis);
                ReadNextToken();
            }
            else if(lastToken == Token.OpenBracket)
            {
                List<Value> values = new List<Value>();
                while (lastToken != Token.CloseBracket) //collect all the arguments
                {
                    ReadNextToken();
                    if (lastToken == Token.Comma || lastToken == Token.CloseBracket)
                    {
                        continue; //just skip the comma's
                    }
                    else
                    {
                        Value v = EvaluateNextExpression();
                        if (v == null)
                        {
                            return null; //this is how to escape the recursive function when a function needs to be evaluated through the main loop first
                        }
                        values.Add(v);
                    }
                }
                ReadNextToken();
                val = new Value(values);
            }
            //this part handles uniary operations
            else
            {
                ReadNextToken();
                Token tok = prevToken;
                val = NextValue().PerformUniaryOperation(tok);
            }
            return val;
        }
    }
}
