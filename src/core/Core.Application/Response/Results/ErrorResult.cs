using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Response.Results;

public class ErrorResult : Result
{
    public ErrorResult(string message) : base(false, message)
    {
    }

    public ErrorResult(string message, Dictionary<string, string[]>? fieldErrors) : base(false, message)
    {
        FieldErrors = fieldErrors;
    }

    public ErrorResult(string message, params object[] messageArgs) : base(false, message, messageArgs)
    {
    }

    public ErrorResult() : base(false)
    {
    }
}
