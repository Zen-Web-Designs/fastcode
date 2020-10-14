using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastcode.parsing;
using fastcode.flib;

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

        public Dictionary<string, Value> Variables { get; private set; } //dictionaries are used for fast access

        public delegate Value BuiltInFunction(List<Value> arguments);
        Dictionary<string, BuiltInFunction> builtInFunctions;
        Dictionary<string, FunctionStructure> functions;

        List<Value> functionResults;
        int procFunctionResults;

        Lexer lexer; //parsing aides
        Token prevToken;
        Token lastToken;
        int bracket_counter; //counts how "deep" you go. Not meant as a dirty joke 
        private Marker expressionMarker; //counts the start of the expresion
        private Marker keywordMarker; //start of keyword

        //this makes up our "call stack" for control structures. Also includes whiles and elses and that stuff rather than functions
        Stack<ControlStructure> CallStack;
        Stack<ControlStructure> UsedStack; //sturctures that have been popped from call stack

        public bool Exit { get; private set; } //exit condition - private set so the program cannot be aborted from the outside without going through an exit function

        private string[] ReadOnlyVariables = { "null", "true", "false", "endl"};

        public Interpreter(TextWriter output, TextReader input, string source)
        { 
            //initialize a bunch of crap
            this.Output = output;
            this.Input = input;
            this.Variables = new Dictionary<string, Value>();
            this.builtInFunctions = new Dictionary<string, BuiltInFunction>();
            this.functions = new Dictionary<string, FunctionStructure>();
            this.CallStack = new Stack<ControlStructure>();
            this.UsedStack = new Stack<ControlStructure>();
            this.functionResults = new List<Value>();
            Variables["null"] = Value.Null;
            Variables["true"] = new Value(1);
            Variables["false"] = new Value(0);
            Variables["endl"] = new Value(Environment.NewLine);
            lexer = new Lexer(source);
            bracket_counter = 0;
            procFunctionResults = 0;
        }

        //starts the program
        public void Start()
        {
            builtInFunctions.Clear();
            CallStack.Clear();
            CallStack.Push(new ControlStructure(ControlStructureType.MainProgram));
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
                        if(Variables.ContainsKey(id))
                        {
                            Variables[id] = v1;
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
                                    if(f.Arguments.ContainsKey(id))
                                    {
                                        f.Arguments[id] = v1;
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
                        Variables.Add(id, v1);
                        break;
                    }
                    else if(lastToken == Token.OpenParenthesis)
                    {
                        if(functions.ContainsKey(id) ||builtInFunctions.ContainsKey(id))
                        {
                            lexer.ShiftCurrentPosition(keywordMarker);
                            ReadNextToken();
                            NextValue();
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
                        if (Variables.ContainsKey(id))
                        {
                            if (Variables[id].Type == ValueType.Array)
                            {
                                Variables[id].Array[(int)v.Double] = setval;
                            }
                            else if (Variables[id].Type == ValueType.String)
                            {
                                if (setval.Type != ValueType.Char)
                                {
                                    throw new Exception("Strings can only index characters.");
                                }
                                char[] str = Variables[id].String.ToCharArray();
                                str[(int)v.Double] = setval.Character;
                                Variables[id] = new Value(new string(str));
                            }
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
                                    if (f.Arguments[id].Type == ValueType.Array)
                                    {
                                        f.Arguments[id].Array[(int)v.Double] = setval;
                                    }
                                    else if (f.Arguments[id].Type == ValueType.String)
                                    {
                                        if (setval.Type != ValueType.Char)
                                        {
                                            throw new Exception("Strings can only index characters.");
                                        }
                                        char[] str = f.Arguments[id].String.ToCharArray();
                                        str[(int)v.Double] = setval.Character;
                                        f.Arguments[id] = new Value(new string(str));
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
                    if(CallStack.Peek().Type == ControlStructureType.MainProgram || CallStack.Peek().Type == ControlStructureType.Function)
                    {
                        throw new UnexpectedKeyword(Token.Break);
                    }
                    else
                    {
                        int i = 0;
                        while (!(CallStack.Peek().Type == ControlStructureType.While || CallStack.Peek().Type == ControlStructureType.Forever || CallStack.Peek().Type == ControlStructureType.Count))
                        {
                            if (CallStack.Peek().Type == ControlStructureType.MainProgram || CallStack.Peek().Type == ControlStructureType.Function)
                            {
                                throw new UnexpectedKeyword(Token.Break);
                            }
                            i++;
                            UsedStack.Push(CallStack.Pop());
                        }
                        ControlStructure current66 = CallStack.Pop();
                        current66.RepeatStatus = ControlStructureRepeatStatus.Return;
                        UsedStack.Push(current66);
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
                    current.Result = (expr1.PerformBinaryOperation(Token.Equals, new Value(0)).Double == 1); //not not the actual result, it just checks if the condition failed so it can skip that section. Kinda misleading if you didn't know - just refer to the assertion token's case.
                    CallStack.Push(current);

                    ReadTillControlStructureStart(false); //read till open bracket

                    if((bool)current.Result == true) //skip till close bracket.
                    {
                        SkipControlStructure();
                        UsedStack.Push(CallStack.Pop());
                    }
                    current.RepeatStatus = ControlStructureRepeatStatus.Return; //sets to return, ifs never repeat
                    
                    break;
                case Token.Else: //not if results are inverted 
                    if (UsedStack.Peek().Type != ControlStructureType.If && UsedStack.Peek().Type != ControlStructureType.Elif)
                    {
                        throw new UnexpectedKeyword(keyword);
                    }
                    else if ((bool)UsedStack.Peek().Result == true) //skip all the crap
                    {
                        CallStack.Push(new ControlStructure(ControlStructureType.Else));
                        ReadTillControlStructureStart(false);
                        ControlStructure current2 = CallStack.Pop();
                        current2.Result = null;
                        current2.RepeatStatus = ControlStructureRepeatStatus.Return;
                        CallStack.Push(current2);
                    }
                    else if ((bool)UsedStack.Peek().Result == false)
                    {
                        CallStack.Push(new ControlStructure(ControlStructureType.Else));
                        ReadTillControlStructureStart(false);
                        SkipControlStructure();
                        ControlStructure current3 = CallStack.Pop();
                        current3.Result = null;
                        current3.RepeatStatus = ControlStructureRepeatStatus.Return;
                        UsedStack.Push(current3);
                    }
                    break;
                case Token.Elif:
                    if(UsedStack.Peek().Type != ControlStructureType.If && UsedStack.Peek().Type != ControlStructureType.Elif)
                    {
                        throw new UnexpectedKeyword(keyword);
                    }
                    else
                    {
                        ControlStructure current5 = new ControlStructure(ControlStructureType.Elif);
                        current5.RepeatStatus = ControlStructureRepeatStatus.Return;
                        CallStack.Push(current5);
                        current5.Result = false;
                        if ((bool)UsedStack.Peek().Result == true)
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
                                UsedStack.Push(CallStack.Pop());
                            }
                        }
                        else
                        {
                            EvaluateNextExpression();
                            ReadTillControlStructureStart(false);
                            SkipControlStructure();
                            UsedStack.Push(CallStack.Pop());
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
                        UsedStack.Push(CallStack.Pop());
                    }
                    break;
                case Token.Count:
                    CountStructure countStructure = new CountStructure();
                    MatchToken(Token.Identifier);
                    string id2 = lexer.TokenIdentifier;
                    ReadNextToken();
                    MatchToken(Token.From);
                    ReadNextToken();
                    try
                    {
                        expr = EvaluateNextExpression();
                        if(expr == null)
                        {
                            return;
                        }
                        countStructure.CountFrom = (int)expr.Double;
                    }
                    catch(InvalidCastException)
                    {
                        throw new Exception("Count ranges must be whole numbers.");
                    }
                    if(!Variables.ContainsKey(id2))
                    {
                        Variables.Add(id2, Value.Null);
                    }
                    Variables[id2] = new Value(countStructure.CountFrom);
                    
                    MatchToken(Token.To);
                    ReadNextToken();
                    expr = EvaluateNextExpression();
                    if (expr == null)
                    {
                        return;
                    }
                    countStructure.CountTo = (int)expr.Double;
                    countStructure.Count = countStructure.CountFrom;
                    CallStack.Push(countStructure);
                    countStructure.IndexerIdentifier = id2;
                    ReadTillControlStructureStart();
                    if(countStructure.Count >= countStructure.CountTo)
                    {
                        SkipControlStructure();
                        UsedStack.Push(CallStack.Pop());
                    }
                    break;
                case Token.Function:
                    MatchToken(Token.Identifier);
                    string fid = lexer.TokenIdentifier;
                    if(functions.ContainsKey(fid) || Variables.ContainsKey(fid) || builtInFunctions.ContainsKey(fid))
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
                            if(argument_identifiers.Contains(lexer.TokenIdentifier) || Variables.ContainsKey(lexer.TokenIdentifier))
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
                    else if(CallStack.Peek().GetType() == typeof(CountStructure))
                    {
                        CountStructure controlStructure = (CountStructure)CallStack.Pop();
                        controlStructure.Count++;
                        if (controlStructure.Count < controlStructure.CountTo)
                        {
                            Variables[controlStructure.IndexerIdentifier] = new Value(controlStructure.Count);
                            CallStack.Push(controlStructure);
                            lexer.ShiftCurrentPosition(CallStack.Peek().StartPosition);
                            ReadNextToken();
                        }
                    }
                    else if(CallStack.Peek().GetType() == typeof(FunctionStructure))
                    {
                        FunctionStructure finishedfunction = (FunctionStructure)CallStack.Pop();
                        finishedfunction.MarkAsFinished();
                        UsedStack.Push(finishedfunction);
                        lexer.ShiftCurrentPosition(finishedfunction.ReturnPosition);
                        functionResults.Add((Value)finishedfunction.Result);
                    }
                    else if (CallStack.Peek().RepeatStatus == ControlStructureRepeatStatus.Return)
                    {
                        UsedStack.Push(CallStack.Pop());
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
                if (lastToken != Token.Newline && lastToken != Token.CloseParenthesis)
                {
                    throw new UnexpectedStatementException(Token.OpenBrace.ToString(), lastToken.ToString());
                }
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

        //this and next value are really important because that's how values for arguments are ascertained
        //gets the next expression (conditions, expressions) and evaluates it. Return's 0 or 1 for conditions
        Value EvaluateNextExpression(int min=0)
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
                if(Variables.ContainsKey(lexer.TokenIdentifier)) //see if it's a variable
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
                        if (Variables[vid].Type == ValueType.String)
                        {
                            val = new Value(Variables[vid].String[(int)v.Double]);
                        }
                        else if (Variables[vid].Type == ValueType.Array)
                        {
                            val = Variables[vid].Array[(int)v.Double];
                        }
                        else
                        {
                            throw new Exception("Only arrays and strings can be indexed.");
                        }
                        MatchToken(Token.CloseBracket);
                    }
                    else
                    {
                        val = Variables[lexer.TokenIdentifier];
                    }
                }
                else if(functions.ContainsKey(lexer.TokenIdentifier) || builtInFunctions.ContainsKey(lexer.TokenIdentifier)) //see if it's a function
                {
                    List<Value> arguments = new List<Value>();
                    ReadNextToken();
                    MatchToken(Token.OpenParenthesis);
                    string fid = lexer.TokenIdentifier;
                    if (procFunctionResults < functionResults.Count)
                    {
                        Value value = functionResults[procFunctionResults];
                        procFunctionResults++;
                        while (lastToken != Token.CloseParenthesis)
                        {
                            ReadNextToken();
                        }
                        ReadNextToken();
                        return value;
                    }
                    while (lastToken != Token.CloseParenthesis) //collect all the arguments
                    {
                        ReadNextToken();
                        if(lastToken == Token.Comma || lastToken == Token.CloseParenthesis)
                        {
                            continue; //just skip the comma's
                        }
                        else
                        {
                            Value v = EvaluateNextExpression();
                            if(v == null)
                            {
                                return null; //this is how to escape the recursive function when a function needs to be evaluated through the main loop first
                            }
                            arguments.Add(v);
                        }
                    }
                    if (functions.ContainsKey(fid))
                    {
                        //val = functions[lexer.TokenIdentifier](this, arguments); //evaluate the value
                        FunctionStructure f = functions[fid].Clone();
                        f.MarkAsExecuting();
                        f.SetArguments(arguments);
                        f.MarkReturnPosition(keywordMarker);
                        f.Result = Value.Null;
                        CallStack.Push(f);
                        lexer.ShiftCurrentPosition(functions[fid].StartPosition);
                        ReadNextToken();
                        return null;
                    }
                    else
                    {
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
                            if(function.Arguments.ContainsKey(lexer.TokenIdentifier))
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
                                    if (function.Arguments[fid].Type == ValueType.String)
                                    {
                                        val = new Value(function.Arguments[fid].String[(int)v.Double]);
                                    }
                                    else if (function.Arguments[fid].Type == ValueType.Array)
                                    {
                                        val = function.Arguments[fid].Array[(int)v.Double];
                                    }
                                    else
                                    {
                                        throw new Exception("Only arrays and strings can be indexed.");
                                    }
                                    MatchToken(Token.CloseBracket);
                                }
                                else
                                {
                                    val = function.Arguments[fid];
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
