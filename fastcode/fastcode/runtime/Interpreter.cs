using fastcode.flib;
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
            {"flib.stdlib", new StandardLibrary() },
            {"flib.mathlib",new MathLibrary() },
            {"flib.linq", new Linq() },
            {"flib.wininterop", new WinInterop()}
        }; //built in libraries allow for interoperability between fastcode and csharp

        public Dictionary<string, Value> GlobalVariables { get; private set; } //dictionaries are used for fast access

        public delegate Value BuiltInFunction(List<Value> arguments);
        Dictionary<string, BuiltInFunction> builtInFunctions;
        Dictionary<string, FunctionStructure> functions;

        Lexer lexer; //parsing aides
        Token prevToken;
        Token lastToken;
        private Marker expressionMarker; //counts the start of the expresion
        private Marker keywordMarker; //start of keyword

        //this makes up our "call stack" for control structures. Also includes whiles and elses and that stuff rather than functions
        Stack<ControlStructure> CallStack;
        ControlStructure prevStructure;
        Debugger debugger;

        public bool Exit { get; set; } //exit condition 

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
            lexer = new Lexer(source);
            debugger = new Debugger(ref CallStack);
            BuiltInLibraries.Add("flib.debugger", debugger);
        }

        //starts the program
        public void Start()
        {
            builtInFunctions.Clear();
            CallStack.Clear();
            GlobalVariables.Clear();
            CallStack.Push(new FunctionStructure("MAINSTRUCTURE"));
            BuiltInLibraries["flib.stdlib"].Install(ref builtInFunctions, this);
            BuiltInLibraries["flib.mathlib"].Install(ref builtInFunctions, this);
            BuiltInLibraries["flib.linq"].Install(ref builtInFunctions, this);
            GlobalVariables["null"] = Value.Null;
            GlobalVariables["true"] = Value.True;
            GlobalVariables["false"] = Value.False;
            GlobalVariables["endl"] = new Value(Environment.NewLine);
            GlobalVariables["doubleType"] = new Value("fastcode.types." + ValueType.Double);
            GlobalVariables["stringType"] = new Value("fastcode.types." + ValueType.String);
            GlobalVariables["arrayType"] = new Value("fastcode.types." + ValueType.Array);
            while(Exit == false) //program loop
            {
                while (lastToken == Token.Newline || lastToken == Token.Unkown)
                {
                    keywordMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
                    ReadNextToken();
                }
                if(lastToken == Token.EndOfFile || CallStack.Count == 0)
                {
                    Exit = true;
                    break;
                }
                ExecuteNextStatement(); //execute a statement
                if (debugger.RequestDebugInterrupt)
                {
                    debugger.StartDebugger(); //used to step next
                }

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

        FunctionStructure GetCurrentFunction()
        {
            Stack<ControlStructure> searched = new Stack<ControlStructure>();
            FunctionStructure function = null;
            while (CallStack.Count > 0)
            {
                searched.Push(CallStack.Pop());
                if (searched.Peek().GetType() == typeof(FunctionStructure))
                {
                    function = (FunctionStructure)searched.Peek();
                    break;
                }
            }
            while (searched.Count > 0)
            {
                CallStack.Push(searched.Pop());
            }
            return function;
        }

        public void ReadTillControlStructureStart(bool markposition=true)
        {
            while (lastToken != Token.OpenBrace)
            {
                ReadNextToken();
            }
            ReadNextToken();

            if(markposition)
            {
                ControlStructure current = CallStack.Pop();
                current.StartPosition = new Marker(lexer.Position.Index - 1, lexer.Position.Collumn, lexer.Position.Row);
                CallStack.Push(current);
            }
        }

        public void SkipControlStructure(int offset = 0, bool readtok=false)
        {
            int braceCount = offset; //skip open params, close params till it balances out
            do
            {
                if (lastToken == Token.OpenBrace)
                {
                    braceCount++;
                }
                else if (lastToken == Token.CloseBrace)
                {
                    braceCount--;
                }
                if (braceCount < 0 && readtok) { break;  }
                ReadNextToken();
            }
            while (braceCount >= 0);
        }

        //executes a single statement
        void ExecuteNextStatement()
        {
            Token keyword = lastToken;
            expressionMarker = new Marker(lexer.Position.Index, lexer.Position.Collumn, lexer.Position.Row);
            ReadNextToken();
            Value expr;
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
                            FunctionStructure f = GetCurrentFunction();
                            if (f.LocalVariables.ContainsKey(id))
                            {
                                f.LocalVariables[id] = v1;
                            }
                            else if (f.Identifier != "MAINSTRUCTURE")
                            {
                                f.LocalVariables.Add(id, v1);
                            }
                            else
                            {
                                GlobalVariables.Add(id, v1);
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
                            EvaluateNextExpression();
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
                            FunctionStructure f = GetCurrentFunction();
                            if (f.LocalVariables[id].Type == ValueType.Array)
                            {
                                f.LocalVariables[id].Array[(int)v.Double] = setval;
                                break;
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
                                break;
                            }
                        }
                    }
                    throw new Exception("Identifier \""+id+"\" cannot stand alone without a keyword.");
                case Token.Break:
                    int i = 0;
                    while (!(CallStack.Peek().GetType() == typeof(WhileStructure) || CallStack.Peek().GetType() == typeof(ForStructure)))
                    {
                        if (CallStack.Peek().GetType() == typeof(FunctionStructure))
                        {
                            throw new UnexpectedStatementException(Token.Break.ToString());
                        }
                        i++;
                        prevStructure = CallStack.Pop();
                    }
                    prevStructure = CallStack.Pop();
                    SkipControlStructure(i);
                    break;
                case Token.Return:
                    FunctionStructure function = null;
                    int j = 0;
                    while (CallStack.Count != 0)
                    {
                        if(CallStack.Peek().GetType() == typeof(FunctionStructure))
                        {
                            function = (FunctionStructure)CallStack.Pop();
                            if(function.Identifier == "MAINSTRUCTURE")
                            {
                                throw new Exception("Only functions may return values.");
                            }
                            break;
                        }
                        CallStack.Pop();
                        j++;
                    }
                    if (PeekNextToken() != Token.Newline && PeekNextToken() != Token.Semicolon)
                    {
                        CallStack.Push(function);
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        function = (FunctionStructure)CallStack.Pop();
                        function.ReturnResult = expr;
                    }
                    CallStack.Push(function);
                    SkipControlStructure(j,true);
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
                    IfElifStructure ifStructure = new IfElifStructure();
                    Value expr1 = EvaluateNextExpression();
                    if(expr1 == null)
                    {
                        return;
                    }
                    ifStructure.Result = (expr1.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1); //not not the actual result, it just checks if the condition failed so it can skip that section. Kinda misleading if you didn't know - just refer to the assertion token's case.
                    CallStack.Push(ifStructure);

                    ReadTillControlStructureStart(false); //read till open bracket

                    if(ifStructure.Result == true) //skip till close bracket.
                    {
                        SkipControlStructure();
                        prevStructure = CallStack.Pop();
                    }
                    
                    break;
                case Token.Else: //not if results are inverted 
                    if (prevStructure.GetType() != typeof(IfElifStructure))
                    {
                        throw new UnexpectedStatementException(keyword.ToString());
                    }
                    IfElifStructure prevElseStructure = (IfElifStructure)prevStructure;
                    if (prevElseStructure.Result == true) //skip all the crap
                    {
                        CallStack.Push(new ElseStructure());
                        ReadTillControlStructureStart(false);
                    }
                    else if (prevElseStructure.Result == false)
                    {
                        ReadTillControlStructureStart(false);
                        SkipControlStructure();
                        prevStructure = new ElseStructure();
                    }
                    break;
                case Token.Elif:
                    if(prevStructure.GetType() != typeof(IfElifStructure))
                    {
                        throw new UnexpectedStatementException(keyword.ToString());
                    }
                    IfElifStructure elifStructure = new IfElifStructure();
                    prevElseStructure = (IfElifStructure)prevStructure;
                    if (prevElseStructure.Result == true)
                    {
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        elifStructure.Result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1);
                        ReadTillControlStructureStart(false);
                        if (elifStructure.Result == true)
                        {
                            SkipControlStructure();
                            prevStructure = elifStructure;
                        }
                        else
                        {
                            CallStack.Push(elifStructure);
                        }
                    }
                    else
                    {
                        ReadTillControlStructureStart(false);
                        SkipControlStructure();
                        prevStructure = elifStructure;
                    }
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
                        whileStructure.WillRepeat = false;
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
                        for (int k = 0; k < expr.String.Length; k++)
                        {
                            forStructure.Values.Add(new Value(expr.String[k]));
                        }
                    }
                    else
                    {
                        throw new Exception("Fastcode can only iterate through an array or string.");
                    }
                    forStructure.currentIndex = 0;
                    FunctionStructure functionStructure1 = GetCurrentFunction();
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

                    CallStack.Push(forStructure);
                    ReadTillControlStructureStart();
                    if(forStructure.currentIndex >= forStructure.Values.Count)
                    {
                        SkipControlStructure();
                        forStructure.WillRepeat = false;
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
                        if(expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1)
                        {
                            lexer.ShiftCurrentPosition(currentpos);
                        }
                        else
                        {
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                        }
                        ReadNextToken();
                    }
                    else if(CallStack.Peek().GetType() == typeof(ForStructure))
                    {
                        ForStructure forStructure2 = (ForStructure)CallStack.Pop();
                        forStructure2.WillRepeat = false;
                        forStructure2.currentIndex++;
                        FunctionStructure functionStructure2 = GetCurrentFunction();
                        if (forStructure2.currentIndex < forStructure2.Values.Count)
                        {
                            functionStructure2.LocalVariables[forStructure2.IndexerIdentifier] = forStructure2.Values[forStructure2.currentIndex];
                            forStructure2.WillRepeat = true;
                            CallStack.Push(forStructure2);
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                        else
                        {
                            functionStructure2.LocalVariables.Remove(forStructure2.IndexerIdentifier);
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(FunctionStructure))
                    {
                        FunctionStructure finishedfunction = (FunctionStructure)CallStack.Pop();
                        finishedfunction.MarkAsFinished();
                        lexer.ShiftCurrentPosition(finishedfunction.ReturnPosition);
                        GetCurrentFunction().functionResults.Enqueue(finishedfunction.ReturnResult);
                    }
                    else
                    {
                        if (CallStack.Peek().WillRepeat)
                        {
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                        else
                        {
                            prevStructure = CallStack.Pop();
                        }
                    }
                    
                    break;
                default:
                    throw new UnexpectedStatementException(keyword.ToString());
            
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
            Value oldval = lexer.TokenValue;
            string oldid = lexer.TokenIdentifier;
            ReadNextToken();
            while(lastToken == Token.Newline)
            {
                ReadNextToken();
            }
            Token tok = lastToken;
            lastToken = old;
            lexer.TokenValue = oldval;
            lexer.TokenIdentifier = oldid;
            lexer.ShiftCurrentPosition(marker);
            return tok;
        }

        Value EvaluateNextExpression()
        {
            FunctionStructure function = GetCurrentFunction();
            Value val = EvaluateNextExpression(0,function);
            if(val == null)
            {
                while(function.processedFunctionResults.Count > 0)
                {
                    function.functionResults.Enqueue(function.processedFunctionResults.Dequeue());
                }
                return null;
            }
            else
            {
                function.processedFunctionResults.Clear();
                return val;
            }
        }

        //this and next value are really important because that's how values for arguments are ascertained
        //gets the next expression (conditions, expressions) and evaluates it. Return's 0 or 1 for conditions
        Value EvaluateNextExpression(int min, FunctionStructure current)
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
            Value value = NextValue(current);
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
                Value rhs = EvaluateNextExpression(nextmin,current);
                if(rhs == null)
                {
                    return null;
                }
                value = value.PerformBinaryOperation(op, rhs);
            }

            return value;
        }

        //gets the next value
        Value NextValue(FunctionStructure current)
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
                        val = GlobalVariables[vid];
                    }
                }
                else if(functions.ContainsKey(lexer.TokenIdentifier) || builtInFunctions.ContainsKey(lexer.TokenIdentifier)) //see if it's a function
                {
                    ReadNextToken();
                    MatchToken(Token.OpenParenthesis);
                    string fid = lexer.TokenIdentifier;
                    int aid = lexer.Position.Index;
                    List<Value> arguments = new List<Value>();
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
                                Value v = EvaluateNextExpression(0,current);
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
                    //bracket_counter++;
                    if (current.functionResults.Count > 0)
                    {
                        Value value = current.functionResults.Dequeue();
                        current.processedFunctionResults.Enqueue(value);
                        ReadNextToken();
                        return value;
                    }
                    if (functions.ContainsKey(fid))
                    {
                        //val = functions[lexer.TokenIdentifier](this, arguments); //evaluate the value
                        if(CallStack.Count > 1000)
                        {
                            throw new StackOverflowException("The call stack's size has exceeded the 1000 item limit.");
                        }
                        FunctionStructure f = functions[fid].Clone();
                        f.MarkAsExecuting();
                        f.SetArguments(arguments);
                        f.MarkReturnPosition(keywordMarker);
                        f.ReturnResult = Value.Null;
                        CallStack.Push(f);
                        lexer.ShiftCurrentPosition(functions[fid].StartPosition);
                        ReadNextToken();
                        return null;
                    }
                    else
                    {
                        val = builtInFunctions[fid].Invoke(arguments);
                        current.processedFunctionResults.Enqueue(val);
                        ReadNextToken();
                        return val;
                    }
                }
                else
                {
                    val = null;
                    FunctionStructure function = GetCurrentFunction();
                    if (function.LocalVariables.ContainsKey(lexer.TokenIdentifier))
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
                val = NextValue(current).PerformUniaryOperation(tok);
            }
            return val;
        }
    }
}
