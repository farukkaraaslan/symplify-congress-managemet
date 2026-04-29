using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Response.Results;

public class SuccessResult : Result
{
    public SuccessResult(string message) : base(true, message)
    {
    }

    public SuccessResult(string message, params object[] messageArgs) : base(true, message, messageArgs)
    {
    }

    public SuccessResult() : base(true)
    {
    }
}
