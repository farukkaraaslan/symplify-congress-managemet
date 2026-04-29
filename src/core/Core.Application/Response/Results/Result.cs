using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Response.Results;

public class Result : IResult
{
    public Result(bool success, string message) : this(success)
    {
        Message = message;
    }

    public Result(bool success, string message, params object[] messageArgs) : this(success)
    {
        Message = message;
        MessageArgs = messageArgs;
    }

    public Result(bool success)
    {
        Success = success;
        Message = string.Empty;
    }

    public bool Success { get; set; }
    public string Message { get; set; }
    public object[]? MessageArgs { get; set; }
    public List<ResultMessageItem>? MessageItems { get; set; }

    /// <summary>Alan bazlı doğrulama hataları (ör. AJAX formlarında data-congress-error-for ile eşleştirme).</summary>
    public Dictionary<string, string[]>? FieldErrors { get; set; }
}
