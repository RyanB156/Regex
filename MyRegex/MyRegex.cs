using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MyRegex
{


    
    public abstract class RegexTerm
    {
        public abstract Parser GetParser();
    }
    
    class RegexTermGroup : RegexTerm
    {
        public List<RegexTerm> Terms { get; private set; }
        public RegexTermGroup(List<RegexTerm> terms) { Terms = terms; }
        public RegexTermGroup() { Terms = new List<RegexTerm>(); }
        public override Parser GetParser()
        {
            return Parsers.ConcatParsers(Terms.Select(t => t.GetParser()).ToList());
        }
    }
    
    abstract class Token : RegexTerm
    {
        public char C { get; protected set; }
        void SetChar(char c) { C = c; }
    }
    
    class AtomicToken : Token // '.', 'a', 'A', '0', etc. {, }, [, ], *, . are "keywords" so these must be escaped (\{ -> atomic {) 
    {
        public AtomicToken(char c) { C = c; }
        public AtomicToken() { }
        public override Parser GetParser()
        {
            return Parsers.PChar(C);
        }
    }
    
    class Wildcard : Token
    {
        public Wildcard(char c) { C = c; }
        public Wildcard() { }
        public static HashSet<char> wildcards = new HashSet<char>() { 's', 'S', 'd', 'D', 'w', 'W', 'i', 'I' };
        public override Parser GetParser()
        {
            switch (C)
            {
                case '.':
                    return Parsers.AnyChar;
                case 's':
                    return Parsers.Choice(new List<Parser>() { Parsers.PChar(' '), Parsers.PChar('\t') });
                case 'S':
                    return Parsers.NotChars(new List<char>() { ' ', '\t', '\r', '\n', '\v', '\f' });
                case 'd':
                    return Parsers.Digit;
                case 'D':
                    return Parsers.NotChars(new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                case 'w':
                    return Parsers.Or(Parsers.Letter, Parsers.Digit);
                case 'W':
                    List<char> blackCharacterList = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    for (int i = 'a'; i <= 'z'; i++)
                        blackCharacterList.Add((char)i);
                    for (int i = 'A'; i <= 'Z'; i++)
                        blackCharacterList.Add((char)i);
                    return Parsers.NotChars(blackCharacterList);
                case 'i':
                    return Parsers.IdentifierChar;
                case 'I':
                    List<char> blackList = new List<char>() { '_' };
                    for (int i = 'a'; i <= 'z'; i++)
                        blackList.Add((char)i);
                    for (int i = 'A'; i <= 'Z'; i++)
                        blackList.Add((char)i);
                    return Parsers.NotChars(blackList);
                default:
                    return Parsers.PChar(C); // "\\" will match '\', "\|" will match '|', etc.
            }
        }
    }
    
    class TokenGroup : RegexTerm
    {
        public List<Token> Tokens { get; private set; }
        public TokenGroup(List<Token> tokens)
        {
            Tokens = tokens;
        }
        public TokenGroup() { }
        public void SetTokens(List<Token> tokens) { Tokens = tokens; }
        public override Parser GetParser()
        {
            return Parsers.ConcatParsers(Tokens.Select(t => t.GetParser()).ToList());
        }
    }
    
    
    class ChoiceRange : Token
    {
        public char M { get; }
        public char N { get; }
        public ChoiceRange(char m, char n)
        {
            if (n < m)
                throw new ArgumentException($"{n} < {m} in range");
            M = m;
            N = n;
        }
        RegexTerm foldRange(List<RegexTerm> terms) // Convert range into nested ors.
        {
            if (terms.Count == 1)
                return terms[0];
            else
                return new Or(terms[0], foldRange(terms.GetRange(1, terms.Count - 1)));

        }
        public override Parser GetParser()
        {
            List<RegexTerm> range = new List<RegexTerm>();
            for (int i = M; i <= N; i++)
            {
                range.Add(new AtomicToken((char)i));
            }
            var flatOrs = foldRange(range);
            return flatOrs.GetParser();
        }
    }
    
    abstract class Quantifier : RegexTerm
    {
        public abstract void SetTerm(RegexTerm term);
    }
    
    class CharQuantifier : Quantifier // a+, ab5*, [a-z]*, ab?6.
    {
        public Char M { get; }
        public RegexTerm RegexTerm { get; private set; }

        public static HashSet<char> validQuantifiers = new HashSet<char> { '*', '+', '?' };

        public CharQuantifier(char m, RegexTerm term)
        {
            M = m;
            RegexTerm = term;
        }
        public CharQuantifier(char m) { M = m; }
        public override void SetTerm(RegexTerm term) { RegexTerm = term; }

        public static bool IsQuantifier(char m)
        {
            return validQuantifiers.Contains(m);
        }

        public override Parser GetParser()
        {
            Parser innerParser = RegexTerm.GetParser();

            switch (M)
            {
                case '*':
                    return Parsers.QuantifiedMatch(innerParser, 0, Int32.MaxValue);
                case '+':
                    return Parsers.QuantifiedMatch(innerParser, 1, Int32.MaxValue);
                case '?':
                    return Parsers.QuantifiedMatch(innerParser, 0, 1, true);

                default:
                    throw new ArgumentException("{0} is not a valid quantifier", M.ToString());
            }
        }
    }

    class Cardinality { }
    class FixedN : Cardinality
    {
        public int N { get; }
        public FixedN(int n) { N = n; }
    }
    class UnboundedN : Cardinality
    {
        public int N { get; }
        public UnboundedN(int n) { N = n; }
    }
    class CardinalRange : Cardinality
    {
        public int M { get; }
        public int N { get; }
        public CardinalRange(int m, int n)
        {
            M = m;
            N = n;
        }
    }
    
    class CardinalQuantifier : Quantifier
    {
        public Cardinality Range { get; private set; }
        public RegexTerm RegexTerm { get; private set; }

        public CardinalQuantifier(Cardinality range, RegexTerm regexTerm)
        {
            Range = range;
            RegexTerm = regexTerm;
        }
        public CardinalQuantifier(Cardinality range) { Range = range; }
        public override void SetTerm(RegexTerm term) { RegexTerm = term; }

        public override Parser GetParser() // N, N->inf, M->N.
        {
            Parser innerParser = RegexTerm.GetParser();
            switch (Range)
            {
                case FixedN fixedN:
                    return Parsers.QuantifiedMatch(innerParser, fixedN.N, fixedN.N, true);
                case UnboundedN unboundedN:
                    return Parsers.QuantifiedMatch(innerParser, unboundedN.N, Int32.MaxValue);
                case CardinalRange range:
                    return Parsers.QuantifiedMatch(innerParser, range.M, range.N, true);
                default:
                    throw new Exception("Bad cardinal range type to Cardinal Quantifier");
            }
        }
    }

    
    class Or : RegexTerm // [ab] matches 'a' or 'b'; [a-z] matches 'a' through 'z'.
    {
        RegexTerm Option1 { get; }
        RegexTerm Option2 { get; }

        public Or(RegexTerm option1, RegexTerm option2)
        {
            Option1 = option1;
            Option2 = option2;
        }

        public override Parser GetParser()
        {
            return Parsers.Or(Option1.GetParser(), Option2.GetParser());
        }
    }

    
    class Choice : RegexTerm
    {
        public bool IsWhiteList { get; }
        public List<Token> Tokens { get; private set; }

        public Choice(bool isWhiteList, List<Token> tokens)
        {
            IsWhiteList = isWhiteList;
            Tokens = tokens;
        }
        public Choice(bool isWhiteList)
        {
            IsWhiteList = isWhiteList;
            Tokens = new List<Token>();
        }

        public override Parser GetParser()
        {
            if (IsWhiteList)
                return Parsers.Choice(Tokens.Select(t => t.GetParser()).ToList());
            else
                return Parsers.NotChars(Tokens.Select(t => t.C).ToList());
            
        }
    }

    class InvalidPatternException : Exception
    {
        public InvalidPatternException() { }
        public InvalidPatternException(string message) : base(message) { }
    }

    public static class Extensions
    {
        public static List<T> Filter<T>(this List<T> sourceList, Func<T, bool> f)
        {
            List<T> list = new List<T>();
            foreach (T t in sourceList)
                if (f(t))
                    list.Add(t);
            return list;
        }           

        public static string Filter(this string source, Func<char, bool> f)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in source)
                if (f(c))
                    sb.Append(c);
            return sb.ToString();
        }

        // Deep clone
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
    }

    

    class MyRegex
    {
        string MatchString { get; }
        public bool IsSuccessfulBuild { get; private set; }
        public List<RegexTerm> PatternTerms { get; private set; }
        Parser Parser;

        public MyRegex(string matchString)
        {
            MatchString = matchString;
            PatternTerms = BuildParser(); // Assigns to the Parser and returns a List<RegexTerm> for debugging.
            
        }

        public void SetParser(Parser parser)
        {
            Parser = parser;
        }

        /*  
         *  <regexp> :- <expr> list
         *  <expr> :- '(', <allterm> list, ')', [<quantifier>] | <qterm>, [<quantifier>] | <allterm>
         *  <qterm> :- <choice> | <token> list
         *  <allterm> :- <qterm> | <or> 
         *  <or> :- <allterm>, '|', <allterm>
         *  <choice> :- '[', ['^'], <choicetoken> list, ']'
         *  <choicetoken> :- <range>, ANYLETTER
         *  <range> :- <literal>, '-', <literal>
         *  <token> :- <literal> | <escapedkeytoken> | <wildcard> | <escapedwildcard>
         *  <quantifier> :- '*', '+', '?', "{n}", "{n,}", "{m,n}"
         *  <literal> :- a-z, A-Z, 0-9, etc. (basically all characters not in ['(', ')', '[', ']', '{', '}', '.', '*', '+', '?', '^']
         *  <escapedkeytoken> :- \(, \), \[, \], \{, \}, \., \\, \*, \+, \?
         *  <wildcard> :- ., 
         *  <escapedwildcard> :- [\s, \S, \d, \D, \w, \W]
         *  ignore whitespaces unless inside "[]"
        */

        public static int level = 0;

        public static bool TryMatchExpr(ParserString parserString, out RegexTerm expr, out ParserString newParserString)
        {
            level++;
            expr = null;
            newParserString = null;
            char c;
            bool doneMatching;

            while (parserString.HasNext())
            {
                doneMatching = false;
                c = parserString.Head;

                if (Char.IsWhiteSpace(c))
                {
                    parserString = parserString.Advance(1);
                    continue;
                }

                // Match nested expr.
                if (c == '(') // Match expr nested in ().
                {
                    if (!parserString.NextContains(')'))
                        return false;

                    parserString = parserString.Advance(1);
                    RegexTermGroup innerTerms = new RegexTermGroup();
                    while (TryMatchExpr(parserString, out RegexTerm innerTerm, out ParserString afterInnerTermString)) // Hopefully this consumes everything up to ')'.
                    {
                        innerTerms.Terms.Add(innerTerm);
                        parserString = afterInnerTermString;
                    }
                    // Empty () is valid. It just won't do anything.

                    while (parserString.HasNext()) // Loop over whitespace.
                    {
                        c = parserString.Head;
                        if (Char.IsWhiteSpace(c)) // Skip whitespace.
                        {
                            parserString = parserString.Advance(1);
                            continue;
                        }

                        if (c == ')')
                        { // Might need to check for whitespace before quantifier.
                            parserString = parserString.Advance(1);
                            if (TryMatchQuantifier(parserString, out Quantifier quantifier, out ParserString afterQuantifierString))
                            {
                                quantifier.SetTerm(innerTerms);
                                expr = quantifier;
                                parserString = afterQuantifierString;
                            }
                            else
                            {
                                expr = innerTerms;
                                parserString = parserString.Advance(1); // Consume ')'.
                            }
                            doneMatching = true;
                        }
                        else // No matching ')' after a token or token and a quantifier.
                            return false;
                    }
                    if (!doneMatching)
                        return false;
                }


                else if (TryMatchQTerm(parserString, out RegexTerm qTerm, out ParserString afterQTermString) && !doneMatching)
                {
                    if (TryMatchQuantifier(afterQTermString, out Quantifier quantifier, out ParserString afterQuantifierString))
                    {
                        quantifier.SetTerm(qTerm);
                        expr = quantifier;
                        parserString = afterQuantifierString;
                        doneMatching =  true;
                    }
                    else // No quantifier found. Keep parserString the same and try a list of tokens instead.
                    {
                        expr = qTerm;
                        parserString = afterQTermString;
                        doneMatching = true;
                    }
                }

                else if (TryMatchAllTerm(parserString, out RegexTerm allTerm, out ParserString afterAllTermString) && !doneMatching)
                {
                    expr = allTerm;
                    parserString = afterAllTermString;
                    doneMatching = true;
                }
                else // No valid (), choice, token, or token list.
                    return false;

                if (doneMatching)
                {
                    while (parserString.HasNext())
                    {
                        c = parserString.Head;
                        if (Char.IsWhiteSpace(c))
                        {
                            parserString = parserString.Advance(1);
                            continue;
                        }

                        else if (c == '|')
                        {
                            parserString = parserString.Advance(1);
                            if (TryMatchExpr(parserString, out RegexTerm secondExpr, out ParserString afterSecondExprString))
                            {
                                expr = new Or(expr, secondExpr);
                                newParserString = afterSecondExprString;
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                            break;
                    }
                    newParserString = parserString;
                    return true;
                }
                throw new Exception("Not done matching!");
            }

            return false;
        }

        public static bool TryMatchCharQuantifier(ParserString parserString, out Quantifier quantifier, out ParserString newParserString)
        {
            quantifier = null;
            newParserString = null;
            while (parserString.HasNext())
            {
                char c = parserString.Head;
                if (Char.IsWhiteSpace(c))
                    parserString = parserString.Advance(1);
                else if (CharQuantifier.IsQuantifier(c))
                {
                    quantifier = new CharQuantifier(c);
                    newParserString = parserString.Advance(1);
                    return true;
                }
                else
                    return false;
            }
            return false;
        }

        public static bool TryMatchBraceQuantifier(ParserString parserString, out Quantifier quantifier, out ParserString newParserString)
        {
            quantifier = null;
            newParserString = null;
            bool firstChar = true;
            string min = "";
            bool inMin = false;
            string max = "";
            bool inMax = false;
            bool inCurlyBraces = false;

            while (parserString.HasNext())
            {
                char c = parserString.Head;
                if (Char.IsWhiteSpace(c))
                {
                    parserString = parserString.Advance(1);
                    continue;
                }
                if (firstChar && c != '{')
                    return false;

                if (!inCurlyBraces && c == '{')
                {
                    firstChar = false;
                    inCurlyBraces = true;
                    inMin = true;
                }
                else if (inMin)
                {
                    if (c == ',')
                    {
                        inMin = false;
                        inMax = true;
                    }
                    else if (c == '}')
                    {
                        quantifier = new CardinalQuantifier(new FixedN(Convert.ToInt32(min))); // No comma. {n}
                        newParserString = parserString.Advance(1);
                        return true;
                    }
                    else if (Char.IsDigit(c))
                    {
                        min += c;
                    }
                    else // Non digit character found when matching the min.
                        return false;
                }
                else if (inMax)
                {
                    if (c == '}')
                    {
                        // Can succeed after 0 or more digits added to max.
                        if (max.Equals(String.Empty))
                            quantifier = new CardinalQuantifier(new UnboundedN(Convert.ToInt32(min))); // {n,}
                        else
                            quantifier = new CardinalQuantifier(new CardinalRange(Convert.ToInt32(min), Convert.ToInt32(max))); // {m,n}
                        newParserString = parserString.Advance(1);
                        return true;
                    }
                    else if (Char.IsDigit(c))
                    {
                        max += c;
                    }
                    else // Non digit character found when matching the min.
                        return false;
                }

                parserString = parserString.Advance(1);
            }
            return false;
        }

        static bool TryMatchQuantifier(ParserString parserString, out Quantifier quantifier, out ParserString newParserString)
        {
            if (TryMatchCharQuantifier(parserString, out quantifier, out newParserString))
                return true;
            else if (TryMatchBraceQuantifier(parserString, out quantifier, out newParserString))
                return true;
            else
                return false;
        }

        public static bool TryMatchQTerm(ParserString parserString, out RegexTerm allTerm, out ParserString newParserString)
        {
            return (TryMatchAllTerm(parserString, out allTerm, out newParserString, false));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parserString"></param>
        /// <param name="allTerm"></param>
        /// <param name="newParserString"></param>
        /// <param name="matchTokenList">True means that the function will match a list of tokens. False means it will match a single token.</param>
        /// <returns></returns>
        public static bool TryMatchAllTerm(ParserString parserString, out RegexTerm allTerm, out ParserString newParserString, bool matchTokenList = true)
        {
            allTerm = null;
            newParserString = null;
            if (TryMatchChoice(parserString, out Choice choice, out ParserString afterChoiceString))
            {
                allTerm = choice;
                newParserString = afterChoiceString;
                return true;
            }
            else
            {
                if (matchTokenList)
                {
                    if (TryMatchTokenList(parserString, out TokenGroup group, out ParserString afterTokenListstring))
                    {
                        allTerm = group;
                        parserString = afterTokenListstring;
                        return true;
                    }
                }
                else
                {
                    if (TryMatchToken(parserString, out Token token, out ParserString afterTokenString))
                    {
                        allTerm = token;
                        newParserString = afterTokenString;
                        return true;
                    }
                }
                
            }
            return false;
        }

        public static bool TryMatchChoice(ParserString parserString, out Choice choice, out ParserString newParserString)
        {
            choice = null;
            newParserString = null;
            char c;
            bool inChoice = false;

            while (parserString.HasNext())
            {
                c = parserString.Head;

                if (Char.IsWhiteSpace(c)) // Skip over whitespace before '['
                {
                    parserString = parserString.Advance(1);
                    continue;
                }
                else
                    break;
            }

            if (parserString.HasNext()) // Match start of the choice block. Make sure it is valid and look for an optional '^' at the beginning.
            {
                c = parserString.Head;

                if (!inChoice) // Starting a choice block.
                {
                    if (c != '[') // The previous loop skips whitespace so this char should be '[' or it isn't a choice block.
                        return false;
                    else // Entering a choice block.
                    {
                        if (!parserString.NextContains(']')) // Incomplete choice block.
                            return false;
                        if (parserString.TryPeek(out char nextChar)) // If the string is empty here then it would be an empty choice block anyway.
                        {
                            if (nextChar == '^')
                            {
                                choice = new Choice(false);
                                parserString = parserString.Advance(2);
                            }
                            else
                            {
                                choice = new Choice(true);
                                parserString = parserString.Advance(1);
                            }
                        }
                        else
                            return false;
                    }
                }
            }

            while (parserString.HasNext())
            {
                // If execution reaches this point then choice should be instantiated and not null.
                c = parserString.Head;

                if (c == ']')
                {
                    newParserString = parserString.Advance(1); // Skip over ']'.
                    return true; // choice has tokens added to it already.
                }
                else
                {
                    if (TryMatchChoiceToken(parserString, out Token choiceToken, out ParserString newCTokenParserString))
                    {
                        choice.Tokens.Add(choiceToken);
                        parserString = newCTokenParserString;
                    }
                    else
                        throw new Exception($"TryMatchChoiceToken failed on {c}"); // This should never fail in theory.
                    
                }
            }

            return false;
        }

        public static bool TryMatchChoiceToken(ParserString parserString, out Token choiceToken, out ParserString newParserString)
        {
            choiceToken = null;
            newParserString = null;
            char c;

            if (parserString.HasNext())
            {
                c = parserString.Head;

                if (c == ']')
                    return false;

                if (parserString.TryPeek(out char nextChar))
                {
                    if (nextChar == '-')
                    {
                        ParserString nextString = parserString.Advance(1); // Temporary string to look at 3rd character.
                        if (nextString.TryPeek(out char endRange)) // Successful range.
                        {
                            if (endRange == ']')
                            {
                                newParserString = parserString.Advance(1);
                                choiceToken = new AtomicToken(c);
                                return true;
                            }
                            choiceToken = new ChoiceRange(c, endRange);
                            newParserString = nextString.Advance(2); // Advance 3 (1 + 2) characters to cover '-' and <end>.
                            return true;
                        }
                        else // Almost a range but there is not enough tokens to complete one.
                        {
                            newParserString = parserString.Advance(1);
                            choiceToken = new AtomicToken(c);
                            return true;
                        }
                    }
                }
                if (c == '\\')
                {
                    if (parserString.TryPeek(out char escapedChar))
                    {
                        if (Wildcard.wildcards.Contains(escapedChar)) // Only wildcard escape characters are special in [].
                        {
                            newParserString = parserString.Advance(2);
                            choiceToken = new Wildcard(escapedChar);
                            return true;
                        }
                    }
                    else
                    {
                        newParserString = parserString.Advance(1);
                        choiceToken = new AtomicToken(c);
                        return true;
                    }
                }
                else
                {
                    choiceToken = new AtomicToken(c);
                    newParserString = parserString.Advance(1);
                    return true;
                }
            }

            return false;
        }

        public static bool TryMatchTokenList(ParserString parserString, out TokenGroup group, out ParserString newParserString, bool matchWhitespace = false)
        {
            group = null;
            newParserString = null;
            List<Token> tempTokens = new List<Token>();
            while (TryMatchToken(parserString, out Token token, out ParserString afterTokenString))
            {
                tempTokens.Add(token);
                parserString = afterTokenString;
            }
            if (tempTokens.Count == 0)
                return false;
            else
            {
                group = new TokenGroup(tempTokens);
                return true;
            }
        }

        public static bool TryMatchToken(ParserString parserString, out Token token, out ParserString newParserString, bool matchWhitespace=false)
        {
            token = null;
            newParserString = null;
            char c;

            if (parserString.HasNext())
            {
                c = parserString.Head;

                if (Char.IsWhiteSpace(c)) // Skips whitespace by default.
                {
                    if (matchWhitespace)
                    {
                        token = new AtomicToken(c);
                        newParserString = parserString.Advance(1);
                        return true;
                    }
                    else
                        parserString = parserString.Advance(1);
                }
                else if (c == '\\')
                {
                    if (parserString.TryPeek(out char escapedChar))
                    {
                        if (Wildcard.wildcards.Contains(escapedChar))
                            token = new Wildcard(escapedChar);
                        else
                            token = new AtomicToken(escapedChar);
                        newParserString = parserString.Advance(2);
                        return true;
                    }
                    else
                        return false; // Invalid escape sequence.
                }
                else if (c == '.')
                {
                    token = new Wildcard(c);
                    newParserString = parserString.Advance(1);
                    return true;
                }
                // These are reserved tokens. The escape sequence above will scoop these up.
                else if (new char[] { '|', '(', ')', '{', '}', '[', ']', '\\', '*', '+', '?' }.Contains(c))
                    return false;
                else
                {
                    token = new AtomicToken(c);
                    newParserString = parserString.Advance(1);
                    return true;
                }
            }
            
            return false;
            
        }

        public List<RegexTerm> BuildParser()
        {
            ParserString parserString = new ParserString(MatchString);
            List<RegexTerm> terms = new List<RegexTerm>();
            while (parserString.HasNext())
            {
                if (TryMatchExpr(parserString, out RegexTerm term, out ParserString afterExprString))
                {
                    terms.Add(term);
                    parserString = afterExprString;
                }
                else
                    throw new Exception("Expression Failure!");
            }

            if (terms.Count > 0)
            {
                Parser foldThen(int i, Parser temp)
                {
                    if (i == terms.Count)
                        return temp;
                    else
                        return foldThen(i + 1, temp.Then(terms[i].GetParser()));
                }

                //Parser = Parsers.ConcatParsers(terms.Select(t => t.GetParser()).ToList());
                Parser = terms.Select(t => t.GetParser()).Aggregate((p1, p2) => Parsers.Then(p1, p2));
                IsSuccessfulBuild = true;
            }
            else
            {
                IsSuccessfulBuild = false;
                Parser = null;
            }
            return terms;            
        }

        /*
         * ".*\\d" Match any characters followed by a single digit 0-9.
         * ".+e.+g" Match "processing"
         *      Match first . to end; Can backtrack 10 chars now.
         *      Backtrack to e and consume e
         *      Match second . to end; Can backtrack 5 chars now.
         *      Backtrack to g and consume g
         *      
         * ".+e.+g\\d" Match "processing2"
         *      Match first . to end
         *      Backtrack in first . to e and consume e
         *      Match second . to end
         *      Backtrack in second . to g and consume g
         *      \\d fails -> backtrack again in second . until g and \\d succeed
         * 
         * (.*) Parser will match to the end of the string then backtrack until the next one succeeds (\\d).
         * 
         *  The +, *, {m}, {m,}, {m,n} quantifiers are the only ones that can consume too many characters.
         *      while inputstring is not empty
         *          if parser is a quantifier
         *              apply the quantifier
         *              
         *              if the next parser fails
         *                  backtrack until the next parser succeeds, as long as it doesn't backtrack more than the tally
         *          else
         *              consume 1 input with the parser
         *              add 1 to tally
         *      
         */

        /*
         * Apply each term one at a time to the input to allow for backtracking.
         * Try to apply the pattern at each point in input to see if any portion of it is a match.
         * 
         * ".+e.+g" "processing"
         */

        struct BackTrackMark
        {
            public int count;
            public int termPosition;

            public BackTrackMark(int c, int t) { count = c; termPosition = t; }
        }

        public bool Run(string input, out string matchResult)
        {
            Stack<BackTrackMark> marks = new Stack<BackTrackMark>();
            ParserString pString = new ParserString(input);
            bool hasSucceeded = false;
            string totalMatched = "";
            matchResult = totalMatched;
            int termPointer;

            while (pString.HasNext()) // Loop and apply the pattern at each position in the input looking for a match.
            {
                termPointer = 0;
                while (termPointer < PatternTerms.Count) // Loop over all of the terms for the pattern.
                {
                    RegexTerm currentTerm = PatternTerms[termPointer];
                    Parser currentParser = currentTerm.GetParser();

                    Result result = currentParser(pString);
                    switch (result)
                    {
                        case Failure f:
                            if (marks.Count > 0) // Can backtrack.
                            {
                                BackTrackMark mark = marks.Pop();
                                totalMatched = totalMatched.Substring(0, totalMatched.Length - 1); // Move back 1 matched character in the total string.
                                termPointer = mark.termPosition; // Start at the RegexTerm right after the quantifier that (maybe) consumed too many characters.
                                mark = new BackTrackMark(mark.count - 1, termPointer);
                                if (mark.count > 0)
                                    marks.Push(mark); // Put the updated backtrack mark back on the stack if it has not been used up.
                                pString = pString.BackTrack(1); // Move the string pointer back 1 char to try again.
                                continue;
                            }
                            else // Match failure. Exit the loop and try again starting at the next character
                            {
                                goto END;
                            }
                            
                        case Success s:
                            if (currentTerm is Quantifier) // Quantifiers can consume too much input and cause the next one to fail.
                            {
                                if (termPointer < PatternTerms.Count - 1)
                                    marks.Push(new BackTrackMark(s.CharsConsumed, termPointer + 1)); // Backtracking <CharsConsumed> chars is now an option.
                            }

                            totalMatched += s.Value;
                            if (!hasSucceeded)
                                hasSucceeded = true; // The pattern has consumed input.
                            pString = s.Remaining;
                            termPointer++;
                            break;
                    }
                    
                }
                if (hasSucceeded) // The pattern has ended after consuming some input.
                {
                    matchResult = totalMatched;
                    return true;
                }
                END:
                pString = pString.Advance(1);
            }

            return false;
        }

        public bool BuildParser(string input, out string matchResult)
        {
            ParserString pString = new ParserString(input);

            // Loop over the input string checking the expression starting from each character.
            while (pString.HasNext())
            {
                if (null == Parser)
                {
                    matchResult = "";
                    return false;
                }

                var result = Parser(pString);

                switch (result)
                {
                    case Success s: // Return early if the match was successful.
                        matchResult = s.Value;
                        return true;
                }
                pString = pString.Advance(1); // Move the cursor forward 1 character.
            }
            matchResult = "";
            return false;
            
        }
    }
}
