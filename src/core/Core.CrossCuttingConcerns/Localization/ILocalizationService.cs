using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Localization;

public interface ILocalizationService
{
    public ICollection<string>? AcceptLocales { get; set; }

    public Task<string> GetLocalizedAsync(string key, string? keySection = null);

    public Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null);
}
