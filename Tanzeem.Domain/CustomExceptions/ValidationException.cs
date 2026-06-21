using System;
using System.Collections.Generic;

namespace Tanzeem.Domain.Exceptions
{
    public class ValidationException : Exception
    {
        public IEnumerable<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string>();
        }

        public ValidationException(IEnumerable<string> errors) : base("There are errors at the entered data, please try again.")
        {
            Errors = errors;
        }
    }
}