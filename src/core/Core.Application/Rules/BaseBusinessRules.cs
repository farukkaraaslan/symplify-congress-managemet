using Core.Application.Response.Results;

namespace Core.Application.Rules;

public static class BaseBusinessRules
{
    public static IResult Run(params IResult[] logics)
    {
        foreach (var result in logics)
        {
            if (!result.Success)
            {
                return result;
            }
        }

        return new SuccessResult();
    }

    public static async Task<IResult> RunAsync(params Func<Task<IResult>>[] logics)
    {
        foreach (var logic in logics)
        {
            var result = await logic();
            if (!result.Success)
            {
                return result;
            }
        }

        return new SuccessResult();
    }
}
