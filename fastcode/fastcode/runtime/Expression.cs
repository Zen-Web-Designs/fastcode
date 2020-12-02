using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fastcode.runtime
{
    public class Variable:ICloneable
    {
        public string Identifier;
        public double Power;

        public override bool Equals(object obj)
        {
            if(obj.GetType() != typeof(Variable))
            {
                return false;
            }
            Variable variable = (Variable)obj;
            return (variable.Identifier == Identifier && variable.Power == Power);
        }

        public Variable(string identifier, double power)
        {
            this.Identifier = identifier;
            this.Power = power;
        }

        public object Clone()
        {
            return new Variable(Identifier, Power);
        }
    }

    public class Term:ICloneable
    {
        public double Coefficient;
        public List<Variable> Variables;
        public Expression Denominator;

        public bool HasDenominator
        {
            get
            {
                return (Denominator != null);
            }
        }

        public bool IsConstant
        {
            get
            {
                foreach(Variable variable in Variables)
                {
                    if(variable.Power != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public static Term operator * (Term a, Term b)
        {
            Term product = (Term)a.Clone();
            product.Coefficient = a.Coefficient * b.Coefficient;
            foreach(Variable bVar in b.Variables)
            {
                bool skip = true;
                foreach(Variable aVar in product.Variables)
                {
                    if(aVar.Identifier == bVar.Identifier)
                    {
                        aVar.Power += bVar.Power;
                        skip = false;
                        break;
                    }
                }
                if(skip)
                {
                    product.Variables.Add((Variable)bVar.Clone());
                }
            }
            product.RemoveZeroPowers();
            if(product.HasDenominator && b.HasDenominator)
            {
                product.Denominator = product.Denominator * b.Denominator;
            }
            else if(b.HasDenominator)
            {
                product.Denominator = (Expression)b.Denominator;
            }
            return product;
        }

        public static Expression operator /(Term a, Term b)
        {
            Term quotient = (Term)a.Clone();
            quotient.Coefficient = a.Coefficient / b.Coefficient;
            foreach (Variable bVar in b.Variables)
            {
                bool skip = true;
                foreach (Variable aVar in quotient.Variables)
                {
                    if (aVar.Identifier == bVar.Identifier)
                    {
                        aVar.Power -= bVar.Power;
                        skip = false;
                        break;
                    }
                }
                if (skip)
                {
                    quotient.Variables.Add(new Variable(bVar.Identifier,-bVar.Power));
                }
            }
            quotient.RemoveZeroPowers();
            Expression total_quotient = new Expression();

            if(b.HasDenominator)
            {
                foreach(Term term in b.Denominator.Terms)
                {
                    total_quotient.Terms.Add(term * quotient);
                }
            }
            else
            {
                total_quotient.Terms.Add(quotient);
            }

            return total_quotient;
        }

        public Term()
        {
            Variables = new List<Variable>();
            Coefficient = 0;
        }

        public Term(double coefficient, List<Variable> variables, Expression denominator)
        {
            this.Variables = variables;
            this.Coefficient = coefficient;
            this.Denominator = denominator;
        }

        public Term(string identifier, double coefficient=1, double power=1)
        {
            this.Coefficient = coefficient;
            Variables = new List<Variable>();
            Variables.Add(new Variable(identifier, power));
        }

        public Term(double constant)
        {
            this.Coefficient = constant;
            Variables = new List<Variable>();
        }

        public Expression Substitute(string identifier, Expression value)
        {
            Term newTerm = new Term();
            newTerm.Coefficient = Coefficient;
            double power = 0;
            foreach(Variable variable in Variables)
            {
                if(variable.Identifier != identifier)
                {
                    newTerm.Variables.Add((Variable)variable.Clone());
                }
                else
                {
                    power += variable.Power;
                }
            }
            Expression expression = value ^power;
            for(int i = 0; i < expression.Terms.Count; i++)
            {
                expression.Terms[i] = expression.Terms[i] * newTerm;
            }
            return expression;
        }

        public Term getNegativePowers()
        {
            Term denominator = new Term();
            foreach(Variable variable in Variables)
            {
                if(variable.Power < 0)
                {
                    denominator.Variables.Add((Variable)variable.Clone());
                }
            }
            return denominator;
        }

        public int RemoveZeroPowers()
        {
            List<Variable> toRemove = new List<Variable>();
            foreach(Variable variable in Variables)
            {
                if(variable.Power == 0)
                {
                    toRemove.Add(variable);
                }
            }
            foreach(Variable variable in toRemove)
            {
                Variables.Remove(variable);
            }
            return toRemove.Count;
        }

        public override bool Equals(object obj)
        {
            if(obj.GetType() != typeof(Term))
            {
                return false;
            }
            Term term = (Term)obj;
            if(!CanAdd(term))
            {
                return false;
            }
            if(term.Coefficient != Coefficient)
            {
                return false;
            }
            return true;
        }

        public bool CanAdd(Term term)
        {
            if(HasDenominator)
            {
                if(Denominator != term.Denominator)
                {
                    return false;
                }
            }
            else if(term.HasDenominator)
            {
                return false;
            }
            if(term.Variables.Count != Variables.Count)
            {
                return false;
            }
            foreach(Variable tvariable in term.Variables)
            {
                bool flag = true;
                foreach(Variable variable in Variables)
                {
                    if(tvariable.Equals(variable))
                    {
                        flag = false;
                        break;
                    }
                }
                if(flag)
                {
                    return false;
                }
            }
            return true;
        }

        public object Clone()
        {
            Term term = new Term();
            term.Coefficient = Coefficient;
            foreach(Variable variable in Variables)
            {
                term.Variables.Add((Variable)variable.Clone());
            }
            if(HasDenominator)
            {
                term.Denominator = (Expression)Denominator.Clone();
            }
            return term;
        }
    }

    public class Expression :ICloneable//, IComparable
    {
        public List<Term> Terms;

        public static Value Substitute(List<Value> arguments)
        {
            if(arguments.Count != 3)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if(arguments[0].Type != ValueType.Expression || arguments[1].Type != ValueType.String || (arguments[2].Type != ValueType.Expression && arguments[2].Type != ValueType.Double))
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Expression replace = null;
            if(arguments[2].Type == ValueType.Double)
            {
                replace = new Expression(arguments[2].Double);
            }
            else
            {
                replace = arguments[2].Expression;
            }
            Expression expression = new Expression();
            foreach(Term term in arguments[0].Expression.Terms)
            {
                expression = expression + term.Substitute(arguments[1].String, replace);
            }
            return new Value(expression);
        }

        public static Expression operator +(Expression a, Expression b)
        {
            Expression sum = (Expression)a.Clone();
            foreach(Term term in b.Terms)
            {
                bool added = false;
                foreach(Term sumterm in sum.Terms)
                {
                    if(sumterm.CanAdd(term))
                    {
                        sumterm.Coefficient += term.Coefficient;
                        added = true;
                        break;
                    }
                }
                if(!added)
                {
                    sum.Terms.Add((Term)term.Clone());
                }
            }
            sum.SimplifyTerms();
            return sum;
        }

        public static Expression operator -(Expression a, Expression b)
        {
            Expression sum = (Expression)a.Clone();
            foreach (Term term in b.Terms)
            {
                bool added = false;
                foreach (Term sumterm in sum.Terms)
                {
                    if (sumterm.CanAdd(term))
                    {
                        sumterm.Coefficient -= term.Coefficient;
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    sum.Terms.Add(new Term(-term.Coefficient,term.Variables, term.Denominator));
                }
            }
            sum.SimplifyTerms();
            return sum;
        }

        public static Expression operator *(Expression a, Expression b)
        {
            Expression product = new Expression();
            foreach(Term aTerm in a.Terms)
            {
                if (aTerm.Denominator == b)
                {
                    product.Terms.Add(new Term(aTerm.Coefficient, aTerm.Variables, null));
                }
                else 
                {
                    foreach (Term bTerm in b.Terms)
                    {
                        Term termProduct = aTerm * bTerm;
                        bool added = false;
                        foreach (Term prodTerm in product.Terms)
                        {
                            if (prodTerm.CanAdd(termProduct))
                            {
                                prodTerm.Coefficient += termProduct.Coefficient;
                                added = true;
                                break;
                            }
                        }
                        if (!added)
                        {
                            product.Terms.Add(termProduct);
                        }
                    } 
                }
            }
            product.SimplifyTerms();
            return product;
        }

        public static Expression operator /(Expression a, Expression b)
        {
            if(b.Terms.Count == 0)
            {
                throw new DivideByZeroException();
            }
            return longDivision(a, b).Item1;
        }

        public static Expression operator %(Expression a, Expression b)
        {
            if (b.Terms.Count == 0)
            {
                throw new DivideByZeroException();
            }
            return longDivision(a, b).Item2;
        }

        public static Expression operator +(Expression a) => a;

        public static Expression operator -(Expression a)
        {
            Expression ret = (Expression)a.Clone();
            foreach(Term term in a.Terms)
            {
                ret.Terms.Add(new Term(-term.Coefficient, term.Variables, term.Denominator));
            }
            return ret;
        }

        public static Expression operator ^(Expression a, double b)
        {
            int count = (int)b;
            Expression expression = new Expression(1);
            for (int i = 0; i < count; i++)
            {
                expression = expression * a;
            }
            return expression;
        }

        private static Tuple<Expression, Expression> longDivision(Expression a, Expression b)
        {
            Expression quotient = new Expression();
            Expression numerator = (Expression)a.Clone();
            b.SimplifyTerms();
            while (numerator.Degree >= b.Degree)
            {
                Expression r = numerator.Terms[0] / b.Terms[0];
                if(r.Terms.Count > 1)
                {
                    break;
                }
                else if(r.Terms[0].HasDenominator || r.Terms[0].getNegativePowers().Variables.Count != 0)
                {
                    break;
                }
                quotient.Terms.Add(r.Terms[0]);
                Expression toRemove = new Expression();
                foreach (Term denominatorTerm in b.Terms)
                {
                    toRemove.Terms.Add(denominatorTerm * r.Terms[0]);
                }
                numerator = numerator - toRemove;
                numerator.SimplifyTerms();
            }
            foreach (Term term in numerator.Terms)
            {
                if (b.Terms.Count == 1)
                {
                    foreach(Term toadd in (term / b.Terms[0]).Terms)
                    {
                        quotient.Terms.Add(toadd);
                    }
                }
                else
                {
                    quotient.Terms.Add(new Term(term.Coefficient, term.Variables, b));
                }
            }
            return new Tuple<Expression, Expression>(quotient, numerator);
        }

        public double Degree
        {
            get
            {
                SimplifyTerms();
                double maxDeg = int.MinValue;
                foreach(Term term in Terms)
                {
                    if (term.Variables.Count == 0)
                    {
                        if(0 > maxDeg)
                        {
                            maxDeg = 0;
                        }
                    }
                    else
                    {
                        foreach (Variable variable in term.Variables)
                        {
                            if (variable.Power > maxDeg)
                            {
                                maxDeg = variable.Power;
                            }
                        }
                    }
                }
                return maxDeg;
            }
        }

        public bool IsZero
        {
            get
            {
                this.SimplifyTerms();
                return (Terms.Count == 0);
            }
        }

        public Expression()
        {
            Terms = new List<Term>();
        }

        public Expression(double constant)
        {
            Terms = new List<Term>();
            Terms.Add(new Term(constant));
        }

        public Expression(string identifier, double coefficient = 1, double power = 1)
        {
            Terms = new List<Term>();
            Terms.Add(new Term(identifier, coefficient, power));
        }

        public Expression(List<Term> terms)
        {
            this.Terms = terms;
        }

        public void SimplifyTerms()
        {
            List<Term> toremove = new List<Term>();
            foreach(Term term in Terms)
            {
                if(term.Coefficient == 0)
                {
                    toremove.Add(term);
                }
            }
            foreach(Term term in toremove)
            {
                Terms.Remove(term);
            }
        }

        public override string ToString()
        {
            string tostr = "";
            bool firstterm = true;
            foreach(Term term in Terms)
            {
                if(term.Coefficient > 0)
                {
                    if(!firstterm)
                    {
                        tostr += " + ";
                    }
                    if (term.HasDenominator)
                    {
                        tostr += "(";
                    }
                    if(!(term.Coefficient == 1 && !term.IsConstant))
                    {
                        tostr += term.Coefficient.ToString();
                    }
                }
                else if(term.Coefficient < 0)
                {
                    tostr += " - ";
                    if (term.HasDenominator)
                    {
                        tostr += "(";
                    }
                    if (!(term.Coefficient == -1 && !term.IsConstant))
                    {
                        tostr += Math.Abs(term.Coefficient).ToString();
                    }
                }
                if(!term.IsConstant)
                {
                    foreach(Variable variable in term.Variables)
                    {
                        if(variable.Power != 0)
                        {
                            if(variable.Power == 1)
                            {
                                tostr += variable.Identifier;
                            }
                            else
                            {
                                tostr += variable.Identifier + "^" + variable.Power;
                            }
                        }
                    }
                }
                if(term.HasDenominator)
                {
                    tostr += ") / ("+term.Denominator+")";
                }
                if (firstterm)
                {
                    firstterm = false;
                }
            }
            return tostr;
        }

        public override bool Equals(object obj)
        {
            if(obj.GetType() != typeof(Expression))
            {
                return false;
            }

            Expression expression = (Expression)obj;

            if(Terms.Count != expression.Terms.Count)
            {
                return false;
            }

            foreach(Term term in Terms)
            {
                bool fail = true;
                foreach(Term tomatch in expression.Terms)
                {
                    if(term.Equals(tomatch))
                    {
                        fail = false;
                        break;
                    }
                }
                if(fail)
                {
                    return false;
                }
            }
            return true;
        }

        public object Clone()
        {
            Expression expression = new Expression();
            foreach(Term term in Terms)
            {
                expression.Terms.Add((Term)term.Clone());
            }
            return expression;
        }
    }
}
