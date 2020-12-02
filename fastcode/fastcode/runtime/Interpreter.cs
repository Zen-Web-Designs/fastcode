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

        public string WorkingDir
        {
            get { return winInterop.WorkingDirectory; }
            set { winInterop.WorkingDirectory = value; }
        }

        public Dictionary<string, Value> GlobalVariables { get; private set; } //dictionaries are used for fast access

        Dictionary<string, BuiltInFunction> builtInFunctions;
        Dictionary<string, FunctionFrame> functions;

        Lexer lexer; //parsing aides
        Token lastToken;
        private Marker expressionMarker; //counts the start of the expresion
        private Marker keywordMarker; //start of keyword

        //this makes up our "call stack" for control structures. Also includes whiles and elses and that stuff rather than functions
        Stack<CallFrame> CallStack;
        CallFrame prevCallFrame;
        Debugger debugger;
        WinInterop winInterop;

        public bool Exit { get; set; } //exit condition 

        private string[] ReadOnlyVariables = { "null", "true", "false", "endl", "doubleType", "stringType", "arrayType" };

        public Interpreter(TextWriter output, TextReader input, string source, string workingDir)
        { 
            //initialize a bunch of crap
            this.Output = output;
            this.Input = input;
            this.GlobalVariables = new Dictionary<string, Value>();
            this.builtInFunctions = new Dictionary<string, BuiltInFunction>();
            this.functions = new Dictionary<string, FunctionFrame>();
            this.CallStack = new Stack<CallFrame>();
            lexer = new Lexer(source);
            debugger = new Debugger(ref CallStack, ref functions, ref builtInFunctions);
            winInterop = new WinInterop(workingDir);
        }

        //starts the program
        public void Start()
        {
            builtInFunctions.Clear();
            CallStack.Clear();
            GlobalVariables.Clear();
            CallStack.Push(new FunctionFrame("MAINSTRUCTURE"));
            (new StandardLibrary()).Install(ref builtInFunctions, this);
            (new MathLibrary()).Install(ref builtInFunctions, this);
            (new Linq()).Install(ref builtInFunctions, this);
            winInterop.Install(ref builtInFunctions, this);
            debugger.Install(ref builtInFunctions, this);
            builtInFunctions.Add("invoke", InvokeUserFunction);
            GlobalVariables["null"] = Value.Null;
            GlobalVariables["true"] = Value.True;
            GlobalVariables["false"] = Value.False;
            GlobalVariables["endl"] = new Value(Environment.NewLine);
            GlobalVariables["doubleType"] = new Value("fastcode.types." + ValueType.Double);
            GlobalVariables["stringType"] = new Value("fastcode.types." + ValueType.String);
            GlobalVariables["arrayType"] = new Value("fastcode.types." + ValueType.Array);
            GlobalVariables["charType"] = new Value("fastcode.types." + ValueType.Character);
            GlobalVariables["nullType"] = new Value("fastcode.types." + ValueType.Null);
            while (Exit == false) //program loop
            {
                while (lastToken == Token.Newline || lastToken == Token.Unkown)
                {
                    keywordMarker = (Marker)lexer.Position.Clone();
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

        public void ImportLibrary(Library library)
        {
            library.Install(ref builtInFunctions, this);
        }

        public void ImportDebuggerCommand(string id,DebuggerCommand command)
        {
            debugger.debuggerCommands.Add(id, command);
        }

        //checks to see if the token is the same as the interpreter expected
        void MatchToken(Token token)
        {
            if(token != lastToken)
            {
                throw new UnexpectedStatementException(token.ToString(), lastToken.ToString());
            }
        }

        FunctionFrame GetCurrentContext()
        {
            Stack<CallFrame> searched = new Stack<CallFrame>();
            FunctionFrame function = null;
            while (CallStack.Count > 0)
            {
                searched.Push(CallStack.Pop());
                if (searched.Peek().GetType() == typeof(FunctionFrame))
                {
                    function = (FunctionFrame)searched.Peek();
                    break;
                }
            }
            while (searched.Count > 0)
            {
                CallStack.Push(searched.Pop());
            }
            return function;
        }

        void readTillCallFrameStart()
        {
            while (lastToken != Token.OpenBrace)
            {
                ReadNextToken();
            }
            CallStack.Peek().StartPosition = (Marker)lexer.Position.Clone();
            ReadNextToken();
        }

        void SkipCallFrame(int offset = 0, bool readtok=false)
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

        //reads the next token
        Token ReadNextToken()
        {
            lastToken = lexer.ReadNextToken();
            return lastToken;
        }

        //Reads the next non-newline token
        Token PeekNextToken()
        {
            Marker marker = (Marker)lexer.Position.Clone();
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

        //executes a single statement
        void ExecuteNextStatement()
        {
            Token keyword = lastToken;
            expressionMarker = (Marker)lexer.Position.Clone();
            ReadNextToken();
            Value expr;
            IfElifFrame prevElifFrame;
            FunctionFrame functionFrame;
            switch (keyword)
            {
                case Token.Global:
                    MatchToken(Token.Identifier);
                    GlobalVariables.Add(lexer.TokenIdentifier, Value.Null);
                    ReadNextToken();
                    break;
                case Token.Abstract:
                    MatchToken(Token.Identifier);
                    GlobalVariables.Add(lexer.TokenIdentifier, new Value(new Expression(lexer.TokenIdentifier)));
                    ReadNextToken();
                    break;
                case Token.Identifier:
                    string id = lexer.TokenIdentifier;
                    if (lastToken == Token.Set)
                    {
                        if(ReadOnlyVariables.Contains(id))
                        {
                            throw new Exception("FastCode cannot write to a read only variable.");
                        }
                        ReadNextToken();
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        if(GlobalVariables.ContainsKey(id))
                        {
                            GlobalVariables[id] =expr;
                            break;
                        }
                        else
                        {
                            functionFrame = GetCurrentContext();
                            if (functionFrame.LocalVariables.ContainsKey(id))
                            {
                                functionFrame.LocalVariables[id] = expr;
                            }
                            else
                            {
                                functionFrame.LocalVariables.Add(id, expr);
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
                            break;
                        }
                    }
                    else if(lastToken == Token.OpenBracket)
                    {
                        ReadNextToken();
                        Value indexValue = EvaluateNextExpression();
                        if (indexValue == null)
                        {
                            return;
                        }
                        if (indexValue.Type != ValueType.Double)
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
                                GlobalVariables[id].Array[(int)indexValue.Double] = setval;
                                break;
                            }
                            else if (GlobalVariables[id].Type == ValueType.String)
                            {
                                if (setval.Type != ValueType.Character)
                                {
                                    throw new Exception("Strings can only index characters.");
                                }
                                char[] str = GlobalVariables[id].String.ToCharArray();
                                str[(int)indexValue.Double] = setval.Character;
                                GlobalVariables[id] = new Value(new string(str));
                                break;
                            }
                        }
                        else
                        {
                            functionFrame = GetCurrentContext();
                            if (functionFrame.LocalVariables[id].Type == ValueType.Array)
                            {
                                functionFrame.LocalVariables[id].Array[(int)indexValue.Double] = setval;
                                break;
                            }
                            else if (functionFrame.LocalVariables[id].Type == ValueType.String)
                            {
                                if (setval.Type != ValueType.Character)
                                {
                                    throw new Exception("Strings can only index characters.");
                                }
                                char[] str = functionFrame.LocalVariables[id].String.ToCharArray();
                                str[(int)indexValue.Double] = setval.Character;
                                functionFrame.LocalVariables[id] = new Value(new string(str));
                                break;
                            }
                        }
                    }
                    throw new Exception("Identifier \""+id+"\" cannot stand alone without a keyword.");
                case Token.Break:
                    int i = 0;
                    while (!(CallStack.Peek().GetType() == typeof(WhileFrame) || CallStack.Peek().GetType() == typeof(ForFrame)))
                    {
                        if (CallStack.Peek().GetType() == typeof(FunctionFrame))
                        {
                            throw new UnexpectedStatementException(Token.Break.ToString());
                        }
                        i++;
                        prevCallFrame = CallStack.Pop();
                    }
                    prevCallFrame = CallStack.Pop();
                    SkipCallFrame(i);
                    break;
                case Token.Return:
                    functionFrame = null;
                    int j = 0;
                    while (CallStack.Count != 0)
                    {
                        if(CallStack.Peek().GetType() == typeof(FunctionFrame))
                        {
                            functionFrame = (FunctionFrame)CallStack.Pop();
                            if(functionFrame.Identifier == "MAINSTRUCTURE")
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
                        CallStack.Push(functionFrame);
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        functionFrame = (FunctionFrame)CallStack.Pop();
                        functionFrame.ReturnResult = expr;
                    }
                    CallStack.Push(functionFrame);
                    SkipCallFrame(j,true);
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
                    
                    ReadNextToken();
                    break;
                case Token.EndOfFile:
                    Exit = true;
                    return;
                case Token.If:
                    IfElifFrame ifFrame = new IfElifFrame();
                    expr = EvaluateNextExpression();
                    if(expr == null)
                    {
                        return;
                    }
                    ifFrame.Result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1); //not not the actual result, it just checks if the condition failed so it can skip that section. Kinda misleading if you didn't know - just refer to the assertion token's case.
                    CallStack.Push(ifFrame);

                    readTillCallFrameStart(); //read till open bracket

                    if(ifFrame.Result == true) //skip till close bracket.
                    {
                        SkipCallFrame();
                        prevCallFrame = CallStack.Pop();
                    }
                    
                    break;
                case Token.Else: //not if results are inverted 
                    if (prevCallFrame.GetType() != typeof(IfElifFrame))
                    {
                        throw new UnexpectedStatementException(keyword.ToString());
                    }
                    prevElifFrame = (IfElifFrame)prevCallFrame;
                    if (prevElifFrame.Result == true) //skip all the crap
                    {
                        CallStack.Push(new ElseFrame());
                        readTillCallFrameStart();
                    }
                    else if (prevElifFrame.Result == false)
                    {
                        CallStack.Push(new ElseFrame());
                        readTillCallFrameStart();
                        SkipCallFrame();
                        prevCallFrame = CallStack.Pop();
                    }
                    break;
                case Token.Elif:
                    if(prevCallFrame.GetType() != typeof(IfElifFrame))
                    {
                        throw new UnexpectedStatementException(keyword.ToString());
                    }
                    IfElifFrame elifFrame = new IfElifFrame();
                    prevElifFrame = (IfElifFrame)prevCallFrame;
                    if (prevElifFrame.Result == true)
                    {
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        elifFrame.Result = (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1);
                        CallStack.Push(elifFrame);
                        readTillCallFrameStart();
                        if (elifFrame.Result == true)
                        {
                            SkipCallFrame();
                            prevCallFrame = CallStack.Pop();
                        }
                    }
                    else
                    {
                        CallStack.Push(elifFrame);
                        readTillCallFrameStart();
                        SkipCallFrame();
                        prevCallFrame = CallStack.Pop();
                    }
                    break;
                case Token.While:
                    WhileFrame whileFrame = new WhileFrame();
                    whileFrame.ExpressionMarker = (Marker)expressionMarker.Clone();
                    expr = EvaluateNextExpression();
                    if(expr == null)
                    {
                        return;
                    }
                    CallStack.Push(whileFrame);
                    readTillCallFrameStart();
                    if (expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1)
                    {
                        SkipCallFrame();
                        prevCallFrame = CallStack.Pop();
                    }
                    break;
                case Token.For:
                    ForFrame forFrame = new ForFrame();
                    MatchToken(Token.Identifier);
                    forFrame.IndexerIdentifier = lexer.TokenIdentifier;
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
                        forFrame.Values = expr.Array;
                    }
                    else if(expr.Type == ValueType.String)
                    {
                        forFrame.Values = new List<Value>();
                        for (int k = 0; k < expr.String.Length; k++)
                        {
                            forFrame.Values.Add(new Value(expr.String[k]));
                        }
                    }
                    else
                    {
                        throw new Exception("Fastcode can only iterate through an array or string.");
                    }
                    forFrame.currentIndex = 0;
                    functionFrame = GetCurrentContext();
                    if (forFrame.Values.Count > 0)
                    {
                        if (functionFrame.LocalVariables.ContainsKey(forFrame.IndexerIdentifier))
                        {
                            functionFrame.LocalVariables[forFrame.IndexerIdentifier] = forFrame.Values[0];
                        }
                        else
                        {
                            functionFrame.LocalVariables.Add(forFrame.IndexerIdentifier, forFrame.Values[0]);
                        }
                    }

                    CallStack.Push(forFrame);
                    readTillCallFrameStart();
                    if(forFrame.currentIndex >= forFrame.Values.Count)
                    {
                        SkipCallFrame();
                        prevCallFrame = CallStack.Pop();
                    }
                    break;
                case Token.Function:
                    MatchToken(Token.Identifier);
                    string fid = lexer.TokenIdentifier;
                    if(functions.ContainsKey(fid) || GlobalVariables.ContainsKey(fid) || builtInFunctions.ContainsKey(fid))
                    {
                        throw new Exception("Identifiers must be unique");
                    }
                    functionFrame = new FunctionFrame(fid);
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
                    functionFrame.SetArgumentParameters(argument_identifiers);
                    CallStack.Push(functionFrame);
                    readTillCallFrameStart();
                    functionFrame = (FunctionFrame)CallStack.Pop();
                    functions[fid] = functionFrame;
                    SkipCallFrame();
                    break;
                case Token.CloseBrace: //checks to return or repeat. 
                    if(CallStack.Peek().GetType() == typeof(WhileFrame))
                    {
                        Marker currentpos = (Marker)lexer.Position.Clone();
                        lexer.ShiftCurrentPosition(((WhileFrame)CallStack.Peek()).ExpressionMarker);
                        ReadNextToken();
                        expr = EvaluateNextExpression();
                        if (expr == null)
                        {
                            return;
                        }
                        if(expr.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1)
                        {
                            prevCallFrame = CallStack.Pop();
                            lexer.ShiftCurrentPosition(currentpos);
                        }
                        else
                        {
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(ForFrame))
                    {
                        ForFrame forStructure2 = (ForFrame)CallStack.Pop();
                        forStructure2.currentIndex++;
                        functionFrame = GetCurrentContext();
                        if (forStructure2.currentIndex < forStructure2.Values.Count)
                        {
                            functionFrame.LocalVariables[forStructure2.IndexerIdentifier] = forStructure2.Values[forStructure2.currentIndex];
                            CallStack.Push(forStructure2);
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                        else
                        {
                            functionFrame.LocalVariables.Remove(forStructure2.IndexerIdentifier);
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(FunctionFrame))
                    {
                        functionFrame = (FunctionFrame)CallStack.Pop();
                        lexer.ShiftCurrentPosition(functionFrame.ReturnPosition);
                        GetCurrentContext().functionResults.Enqueue(functionFrame.ReturnResult);
                    }
                    else
                    {
                        prevCallFrame = CallStack.Pop();
                    }

                    break;
                default:
                    throw new UnexpectedStatementException(keyword.ToString());
            
            }
            if(lastToken == Token.Semicolon)
            {
                keywordMarker = (Marker)lexer.Position.Clone();
                ReadNextToken();
                while (lastToken == Token.Newline)
                {
                    ReadNextToken();
                }
                ExecuteNextStatement();
            }
        }


        Value EvaluateNextExpression()
        {
            FunctionFrame function = GetCurrentContext();
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
        Value EvaluateNextExpression(int min, FunctionFrame current, bool multiterm = true, bool candivide = true)
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
                if(!multiterm && precedens[lastToken] < 3)
                {
                    break;
                }
                Token op = lastToken;
                if (!candivide && op == Token.Slash)
                {
                    break;
                }
                int prec = precedens[lastToken]; // Operator Precedence
                int assoc = 0; // 0 left, 1 right; Operator associativity
                int nextmin = assoc == 0 ? prec : prec + 1;
                ReadNextToken();
                bool divide = true;
                if(op == Token.Slash)
                {
                    divide = false;
                }
                Value rhs = EvaluateNextExpression(nextmin,current,false,divide);
                if(rhs == null)
                {
                    return null;
                }
                value = value.PerformBinaryOperation(op, rhs);
            }

            return value;
        }

        //gets the next value
        Value NextValue(FunctionFrame current)
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
                        Value v = EvaluateNextExpression(0,current);
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
                        if (CallStack.Count > 1000)
                        {
                            throw new StackOverflowException("The call stack's size has exceeded the 1000 item limit.");
                        }
                        FunctionFrame f = functions[fid].CloneTemplate();
                        f.SetArguments(arguments);
                        f.ReturnPosition = (Marker)keywordMarker.Clone();
                        f.ReturnResult = Value.Null;
                        CallStack.Push(f);
                        lexer.ShiftCurrentPosition(functions[fid].StartPosition);
                        ReadNextToken();
                        return null;
                    }
                    else
                    {
                        val = builtInFunctions[fid].Invoke(arguments);
                        ReadNextToken();
                        if (val == null)
                        {
                            return null;
                        }
                        current.processedFunctionResults.Enqueue(val);
                        return val;
                    }
                }
                else
                {
                    val = null;
                    FunctionFrame function = GetCurrentContext();
                    if (function.LocalVariables.ContainsKey(lexer.TokenIdentifier))
                    {
                        string fid = lexer.TokenIdentifier;
                        if (PeekNextToken() == Token.OpenBracket)
                        {
                            ReadNextToken();
                            ReadNextToken();
                            Value v = EvaluateNextExpression(0,current);
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
                val = EvaluateNextExpression(0,current); //call the evaluate function
                if (val == null)
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
                        Value v = EvaluateNextExpression(0,current);
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
                Token tok = lastToken;
                ReadNextToken();
                if (tok == Token.Refrence)
                {
                    val = new Value(lexer.TokenIdentifier);
                }
                else
                {
                    Value val1 = NextValue(current);
                    if (val1 == null)
                    {
                        return null;
                    }
                    val = val1.PerformUniaryOperation(tok);
                }
            }
            return val;
        }

        public Value InvokeUserFunction(List<Value> arguments)
        {
            if(!(arguments.Count >= 1))
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if(arguments[0].Type != ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            if (CallStack.Count > 1000)
            {
                throw new StackOverflowException("The call stack's size has exceeded the 1000 item limit.");
            }
            FunctionFrame f = functions[arguments[0].String].CloneTemplate();
            if(arguments.Count == 1)
            {
                f.SetArguments(new List<Value>());
            }
            else
            {
                f.SetArguments(arguments.GetRange(1,arguments.Count-1));
            }
            f.ReturnPosition = (Marker)keywordMarker.Clone();
            f.ReturnResult = Value.Null;
            CallStack.Push(f);
            lexer.ShiftCurrentPosition(functions[arguments[0].String].StartPosition);
            return null;
        }
    }
}