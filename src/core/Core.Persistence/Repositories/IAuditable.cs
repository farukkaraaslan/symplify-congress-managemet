using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Repositories;

public interface IAuditable
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    string? DeletedBy { get; set; }
}
