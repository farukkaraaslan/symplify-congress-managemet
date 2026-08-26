using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TransactionStatusPhaseRepository : EfRepositoryBase<TransactionStatusPhase, BackOfficeDbContext, int>, ITransactionStatusPhaseRepository
{
    public TransactionStatusPhaseRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
