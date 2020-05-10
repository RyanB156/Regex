using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using static MyRegex.Parsers;

namespace MyRegex
{
    class Program
    {
        static void TestRegex()
        {
            string pattern = "[abc]+bc";

            /* Buglist:
             * List matching rules are too greedy. They need to stop so that the next terms succeed, if any.
             * "a+." fails on "aaaa" but not on "aaab" because a+ matches to the end.
             */

            List<string> testStrings = new List<string> { "processing", "aaaaa", "\\^2", "abc123", "123abc", "aabbcc", "cde1b2c3", "d.txt", "word10", "12dddaaa24", "aaaaaaaaaab" };


            Console.WriteLine(new String('*', 10));
            foreach (var s in testStrings)
            {
                try
                {
                    Console.WriteLine("Regex:{0} => {1}", s, new Regex(pattern).Match(s).Value);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Regex:{0} => {1}", s, e.Message);
                }

                MyRegex r = new MyRegex(pattern);
                r.Run(s, out string myResult);
                Console.WriteLine("Mine :{0} => {1}\n", s, myResult ?? "Regex Failure!");
            }
        }

        static void TestBraceMatch()
        {
            string[] testStrings = { " ", "a", ",", "{", "{ ", "{,", "{a", "{1", "{5}", "{3,}", "{0,9}", "{ 1 , 5 }" };
            foreach (string str in testStrings)
            {
                if (MyRegex.TryMatchBraceQuantifier(new ParserString(str), out Quantifier result, out ParserString parserString))
                {
                    Console.WriteLine("{0} => Success: {1}", str, result);
                }
                else
                {
                    Console.WriteLine("{0} => Failure", str);
                }

            }
        }

        static void TestQuantifierMatch()
        {
            string[] testStrings = { "a", "1", "+", "*", "?" };
            foreach (string str in testStrings)
            {
                if (MyRegex.TryMatchCharQuantifier(new ParserString(str), out Quantifier result, out ParserString parserString))
                {
                    Console.WriteLine("{0} => Success: {1}", str, result);
                }
                else
                {
                    Console.WriteLine("{0} => Failure", str);
                }
            }
        }

        static void TestMatchToken()
        {
            string[] testStrings = { "abc", "a.b?", "\\", "\\s", "\\a", ".", "\\(" , "abc|def", "a|b|c|d|e"};
            foreach (string str in testStrings)
            {
                if (MyRegex.TryMatchToken(new ParserString(str), out Token t, out ParserString nP))
                {
                    Console.WriteLine("{0} => Token: {1}", str, t.C);
                }
                else
                    Console.WriteLine("{0} => Token Failure", str);
            }
        }

        public static void TestMatchChoiceToken()
        {
            string[] testStrings = { " ", ".", "abc", "a-", "a-z", " -5", "\\t", "\\d", "(" };
            foreach (string str in testStrings)
            {
                if (MyRegex.TryMatchChoiceToken(new ParserString(str), out Token token, out ParserString newParserString))
                    Console.WriteLine("{0} => Token: {1}", str, token);
                else
                    Console.WriteLine("{0} => Token Failure", str);
            }
        }

        public static void TestMatchChoice()
        {
            string[] testStrings = { "[ ]", "[.]", "[abc[", "]a-", "[a-z]", "[ -5]", "[]\\t]", "[\\d", "(" , "[[[[]"};
            foreach (string str in testStrings)
            {
                if (MyRegex.TryMatchChoice(new ParserString(str), out Choice choice, out ParserString newParserString))
                    Console.WriteLine("{0} => Choice: {1}", str, choice);
                else
                    Console.WriteLine("{0} => Choice Failure", str);
            }
        }

        static void TestBuildParser()
        {
            MyRegex myRegex = new MyRegex("ab?c*"); // Skips over 'a' for some reason...
            List<RegexTerm> terms = myRegex.BuildParser();
            terms.ForEach(t => Console.WriteLine(t));
        }

        static void ParserStringTest()
        {
            ParserString p = new ParserString("abcdefg");
            char c;
            while (p.HasNext())
            {
                c = p.Head;
                Console.WriteLine(c);
                p = p.Advance(1);
            }
        }

        static void TestEmail()
        {
            //string pattern = @"[a-z0-9.-]+@[\w.-]+\.[a-z.]{2,6}";
            string pattern = @"[a-z0-9.-]+@[\w.-]+\.[a-z.]{2,6}";

            string s = "bressetteryan@gmail.com";

            Regex regex = new Regex(pattern);
            Console.WriteLine("Regex => {0}", regex.Match(s));
            

            MyRegex r = new MyRegex(pattern);
            r.Run(s, out string myResult);
            Console.WriteLine("Mine :{0} => {1}\n", s, myResult ?? "Regex Failure!");
        }

        static void TestQuantifiedMatch()
        {
            // ?, +, *, {2}, {2,}, {2,5}
            Parser p = new Wildcard('.').GetParser();
            List<Tuple<string, Parser>> parserPairs = new List<Tuple<string, Parser>>() {
                    new Tuple<string,Parser>("?", Parsers.QuantifiedMatch(p, 0, 1, true)),
                    new Tuple<string,Parser>("+", Parsers.QuantifiedMatch(p, 1, Int32.MaxValue)),
                    new Tuple<string,Parser>("*", Parsers.QuantifiedMatch(p, 0, Int32.MaxValue)),
                    new Tuple<string,Parser>("{3}", Parsers.QuantifiedMatch(p, 3, 3, true)),
                    new Tuple<string,Parser>("{3,}", Parsers.QuantifiedMatch(p, 3, Int32.MaxValue)),
                    new Tuple<string,Parser>("{3,5}", Parsers.QuantifiedMatch(p, 3, 5, true))
                };

            string test = "aaabcabc";
            int charsConsumed;
            foreach (var pair in parserPairs)
            {
                Result r = pair.Item2(new ParserString(test));
                switch (r)
                {
                    case Success s:
                        charsConsumed = s.CharsConsumed;
                        break;
                    default:
                        charsConsumed = 0;
                        break;
                }
                Console.WriteLine("{0} => {1}, {2} consumed", pair.Item1, r, charsConsumed);
            }
        }

        static void Main(string[] args)
        {
            bool inConsole = false;

            if (args.Length == 2)
            {
                inConsole = true;

                MyRegex r = new MyRegex(args[0]);
                if (r.Run(args[1], out string result))
                    Console.WriteLine("\"{0}\"", result);
                else
                    Console.WriteLine("Failure");
            }
            else if (args.Length == 1)
            {
                inConsole = true;
                MyRegex r = new MyRegex(args[0]);
                if (r.IsSuccessfulBuild)
                    r.PatternTerms.ForEach(term => Console.WriteLine(term));
            }
            else
            {
                //TestBuildParser();

                //TestRegex();
                //TestQuantifiedMatch();
                TestEmail();
                //ParserStringTest();

            }


            if (!inConsole)
                Console.ReadKey(true);
        }
    }
}
