using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Response.Results;

public class SuccessDataResult<T> : DataResult<T>
{
    public SuccessDataResult(T data, string message) : base(data, true, message)
    {
    }

    public SuccessDataResult(T data, string message, params object[] messageArgs) : base(data, true, message, messageArgs)
    {
    }

    public SuccessDataResult(T data) : base(data, true)
    {
    }

    public SuccessDataResult(string message) : base(default!, true, message)
    {

    }

    public SuccessDataResult(string message, params object[] messageArgs) : base(default!, true, message, messageArgs)
    {
    }

    public SuccessDataResult() : base(default!, true)
    {

    }
}
