using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRegex
{
    public class Result
    {
        public bool IsSuccess { get; protected set; }
    }
    public class Success : Result
    {
        public string Value { get; }
        public int CharsConsumed { get; private set; }
        public ParserString Remaining { get; }
        public Success(string value, ParserString remaining)
        {
            Value = value;
            CharsConsumed = 1;
            Remaining = remaining;
            IsSuccess = true;
        }

        public Success(string value, ParserString remaining, int charsConsumed) : this(value, remaining)
        {
            if (charsConsumed < 0)
                throw new ArgumentException("The number of characters must be greater than or equal to zero");
            CharsConsumed = charsConsumed;
        }

        public Success WithConsumedAs(int charsConsumed)
        {
            if (charsConsumed < 0)
                throw new ArgumentException("The number of characters must be greater than or equal to zero");
            CharsConsumed = charsConsumed;
            return new Success(Value, Remaining, charsConsumed);
        }

        public override string ToString()
        {
            return "\"" + Value + " Consumed:" + CharsConsumed + "\"";
        }
    }
    public class Failure : Result
    {
        public Failure() { IsSuccess = false; }
        public override string ToString()
        {
            return "Failure";
        }
    }

    public abstract class TResult<T>
    {
        public bool IsSuccess { get; protected set; }
    }
    public class TSuccess<T> : TResult<T>
    {
        public T Value { get; }
        public TSuccess(T value) { IsSuccess = true; Value = value; }
    }
    public class TFailure<T> : TResult<T>
    {
        public string Message { get; }
        public TFailure (string message) { IsSuccess = false; Message = message; }
    }
}

public abstract class Option<T> { }
public class Some<T> : Option<T>
{
    T Value { get; }
    public Some(T value) { Value = value; }
}
public class None<T> : Option<T> { }

