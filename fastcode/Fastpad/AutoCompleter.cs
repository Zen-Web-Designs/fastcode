using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using fastcode.parsing;

namespace Fastpad
{
    public class AutoCompleter
    {
        public static char[] LeftBoundChars =
        {
            '(',
            '[',
            '{',
        };

        public static char[] RightBoundChars =
        {
            ')',
            ']',
            '}',
        };

        public static char[] NuetralBoundChars =
        {
            ' ',
            '\r',
            '\t',
            '\n',
            '+',
            '-',
            '*',
            '/',
            '%',
            '^',
            '=',
            '&',
            '|'
        };

        public static string[] Keywords =
        {
            "and",
            "or",
            "in",
            "if",
            "else",
            "elif",
            "for",
            "while",
            "function",
            "break",
            "return",
            "stop",
            "import",
            "global",
            "rem",
            "\"flib.debugger\"",
            "\"flib.wininterop\"",
            "\"flib.math.polynomials\""
        };

        public static string SelectCurrentKeyword(RichTextBox textBox)
        {
            string selected_text = string.Empty;
            int i = textBox.SelectionStart - 1;
            while (i >= 0 && !LeftBoundChars.Contains(textBox.Text[i]) && !NuetralBoundChars.Contains(textBox.Text[i]))
            {
                selected_text = textBox.Text[i] + selected_text;
                i--;
            }
            i = textBox.SelectionStart;
            while (i < textBox.TextLength && !RightBoundChars.Contains(textBox.Text[i]) && !NuetralBoundChars.Contains(textBox.Text[i]))
            {
                selected_text = selected_text + textBox.Text[i];
                i++;
            }
            return selected_text;
        }

        public static void CompleteCurrentKeyword(RichTextBox textBox,string complete_keyword)
        {
            int lower_bound = textBox.SelectionStart - 1;
            while (lower_bound > 0 && !LeftBoundChars.Contains(textBox.Text[lower_bound-1]) && !NuetralBoundChars.Contains(textBox.Text[lower_bound-1]))
            {
                lower_bound--;
            }
            int upper_bound = textBox.SelectionStart;
            while (upper_bound < textBox.TextLength && !RightBoundChars.Contains(textBox.Text[upper_bound]) && !NuetralBoundChars.Contains(textBox.Text[upper_bound]))
            {
                upper_bound++;
            }
            textBox.Select(lower_bound, upper_bound - lower_bound);
            textBox.SelectedText = complete_keyword +" ";
            textBox.SelectionStart += textBox.SelectionLength;
            textBox.SelectionLength = 0;
        }

        List<string> identifiers;
        List<string> imported_libraries;

        public AutoCompleter()
        {
            identifiers = new List<string>();
            imported_libraries = new List<string>();
        }

        public void ScanForIdentifiers(RichTextBox textBox)
        {
            identifiers.Clear();
            imported_libraries.Clear();
            Lexer lexer = new Lexer(textBox.Text);
            Token token = Token.Unkown;
            Token prevToken = Token.Unkown;
            while ((token = lexer.ReadNextToken()) != Token.EndOfFile)
            {
                if(token == Token.Identifier && SelectCurrentKeyword(textBox) != lexer.TokenIdentifier && !identifiers.Contains(lexer.TokenIdentifier))
                {
                    identifiers.Add(lexer.TokenIdentifier);
                }
                else if(prevToken == Token.Import)
                {
                    imported_libraries.Add(lexer.TokenValue.String);
                }
                prevToken = token;
            }
        }

        private int strcmp(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }

        public bool IsKeyword(string key)
        {
            if(Keywords.Contains(key))
            {
                return true;
            }
            return false;
        }

        public string[] SearchForSuggestions(string key, int limit=-1)
        {
            List<string> keywords = new List<string>();
            keywords.AddRange(Keywords);
            keywords.AddRange(identifiers);
            while(true)
            {
                bool flag = false;
                for (int i = 0; i < keywords.Count-1; i++)
                {
                    if(strcmp(keywords[i],key) > strcmp(keywords[i+1],key))
                    {
                        string temp = keywords[i];
                        keywords[i] = keywords[i + 1];
                        keywords[i + 1] = temp;
                        flag = true;
                    }
                }
                if(!flag)
                {
                    if(limit == -1)
                    {
                        return keywords.ToArray();
                    }
                    else if(keywords.Count > limit)
                    {
                        string[] limited = new string[limit];
                        for (int i = 0; i < limit; i++)
                        {
                            limited[i] = keywords[i];
                        }
                        return limited;
                    }
                }
            }
        }
    }
}
