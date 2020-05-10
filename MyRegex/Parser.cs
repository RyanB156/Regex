using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRegex
{
    public delegate Result Parser(ParserString input);

    public static class Parsers
    {

        /// <summary>
        /// Parse any one character from the input stream.
        /// </summary>
        public static Parser AnyChar = input =>
        {
            if (null == input | input.IsEmpty())
                return new Failure();
            else return new Success(input.Head.ToString(), input.Advance(1));
        };

        /// <summary>
        /// Parse a letter from the input stream.
        /// </summary>
        public static Parser Letter = input =>
        {
            if (null == input | input.IsEmpty())
                return new Failure();
            else if (Char.IsLetter(input.Head))
                return new Success(input.Head.ToString(), input.Advance(1));
            else
                return new Failure();
        };

        /// <summary>
        /// Parse a digit from the input stream.
        /// </summary>
        public static Parser Digit = input =>
        {
            if (null == input | input.IsEmpty())
                return new Failure();
            else if (Char.IsDigit(input.Head))
                return new Success(input.Head.ToString(), input.Advance(1));
            else
                return new Failure();
        };

        /// <summary>
        /// Parse the specified character from the input stream.
        /// </summary>
        public static Func<char, Parser> PChar = c => input =>
        {
            if (null == input | input.IsEmpty())
                return new Failure();
            else if (c == input.Head)
                return new Success(c.ToString(), input.Advance(1));
            else
                return new Failure();
        };

        /// <summary>
        /// Parse anything but the specified character.
        /// </summary>
        public static Func<char, Parser> NotChar => c => input =>
        {
            if (null == input || !input.HasNext())
                return new Failure();
            else if (c != input.Head)
                return new Success(c.ToString(), input.Advance(1));
            else
                return new Failure();
        };

        /// <summary>
        /// Parse any character that is not in the specified list.
        /// </summary>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static Parser NotChars(List<char> cs) => input =>
        {
            if (cs.Contains(input.Head))
                return new Failure();
            else
                return new Success(input.Head.ToString(), input.Advance(1));
        };

        /// <summary>
        /// Parse whitespace characters from the input stream.
        /// </summary>
        public static Parser WhiteSpace = input =>
        {
            if (null == input | input.IsEmpty())
                return new Failure();
            else if (Char.IsWhiteSpace(input.Head))
                return new Success(input.Head.ToString(), input.Advance(1));
            else
                return new Failure();
        };

        /// <summary>
        /// Parser combinator that applies the first parser then the second one if the first parser succeeds.
        /// </summary>
        /// <param name="parser1">The first parser to apply.</param>
        /// <param name="parser2">The second parser to apply.</param>
        /// <returns>A parser that applies two parsers one after the other and returns a success or failure result.</returns>
        public static Parser Then(this Parser parser1, Parser parser2) => input =>
        {
            var res1 = parser1(input);
            switch (res1)
            {
                case Success s1:
                    string str1 = s1.Value;
                    var res2 = parser2(s1.Remaining);
                    switch (res2)
                    {
                        case Success s2:
                            string str2 = s2.Value;
                            return new Success(str1 + str2, s2.Remaining);
                        case Failure f2:
                        default:
                            return new Failure();
                    }
                case Failure s1:
                default:
                    return new Failure();
            }
        };

        // Supports any combination of min and max to represent all regex quantifiers.
        public static Parser QuantifiedMatch(Parser parser, int min, int max, bool limitedMax=false) => input =>
        {
            Result lastResult = new Failure();
            string totalMatched = "";
            int successCount = 0;
            Func<bool> failFun;
            bool endLoop = false;

            if (!limitedMax)
                failFun = () => (successCount >= min);
            else
                failFun = () => (successCount >= min && successCount <= max);

            while (input.HasNext() && !endLoop)
            {
                switch (parser(input))
                {
                    case Failure f:
                        if (min == 0 && successCount == 0)
                            return new Success("", input, 0);
                        if (failFun())
                        {
                            return lastResult;
                        }
                        else
                            return f;
                    case Success s:
                        successCount++;
                        totalMatched += s.Value;
                        if (limitedMax && successCount > max)
                        {
                            return lastResult;
                        }
                        lastResult = new Success(totalMatched, s.Remaining, successCount);
                        
                        input = s.Remaining;
                        if (!input.HasNext())
                            goto END;
                        break;
                }
            }
            END:
            endLoop = true;
            if (lastResult is Success success)
                return success.WithConsumedAs(successCount);
            else
                return lastResult;
        };

        /// <summary>
        /// Create a parser by chaining the specified parsers together.
        /// </summary>
        /// <param name="parsers"></param>
        /// <returns></returns>
        public static Parser ConcatParsers(List<Parser> parsers)
        {
            if (null == parsers | parsers.Count == 0)
                throw new ArgumentException();
            if (parsers.Count == 1)
                return parsers[0];
            else
                return parsers.Aggregate((p1, p2) => p1.Then(p2));
        }


        public static Parser Optional(Parser parser) => input =>
        {
            switch (parser(input))
            {
                case Success s:
                    return s;
                default:
                    return new Success("", input);
            }
        };

        /// <summary>
        /// Applies either the first parser or the second parser.
        /// </summary>
        /// <param name="parser1"></param>
        /// <param name="parser2"></param>
        /// <returns></returns>
        public static Parser Or(Parser parser1, Parser parser2) => input =>
        {
            switch (parser1(input))
            {
                case Success s1:
                    return s1;
                case Failure f1:
                    switch (parser2(input))
                    {
                        case Success s2:
                            return s2;
                        case Failure f2:
                            return f2;
                    }
                    return f1;
            }
            return new Failure();
        };


        public static Parser Choice(List<Parser> parsers) => input =>
        {
            foreach (Parser p in parsers) // Try all parsers in the list.
            {
                switch (p(input)) // If one is successful then return the result.
                {
                    case Success s:
                        return s;
                }
            }
            return new Failure(); // Else return a failure.
        };

        public static Parser IdentifierChar = Or(Letter, PChar('_'));
        public static Parser Identifier = QuantifiedMatch(IdentifierChar, 1, Int32.MaxValue);

    }
}
