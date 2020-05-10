using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRegex
{
    public class ParserString
    {
        public string InputString { get; }
        int pointer = 0;
        int length;

        public ParserString(string input)
        {
            InputString = input;
            length = input.Length;
        }

        private ParserString(string input, int pointer, int length)
        {
            InputString = input;
            this.pointer = pointer;
            this.length = length;
        }

        public bool IsEmpty() => length == 0;
        public bool HasNext() => length > 0;
        public bool NextContains(char c)
        {
            string restString = InputString.Substring(pointer);
            return restString.Contains(c);
        }
        public char Head { get { return InputString[pointer]; } }
        public bool TryPeek(out char nextChar)
        {
            if (length > 1)
            {
                nextChar = InputString[pointer + 1];
                return true;
            }
            else
            {
                nextChar = '\0';
                return false;

            }
        }
        public ParserString Advance(int distance)
        {
            return new ParserString(InputString, pointer + distance, length - distance);
        }
        public ParserString BackTrack(int distance)
        {
            return new ParserString(InputString, pointer - distance, length + distance);
        }
    }
}
