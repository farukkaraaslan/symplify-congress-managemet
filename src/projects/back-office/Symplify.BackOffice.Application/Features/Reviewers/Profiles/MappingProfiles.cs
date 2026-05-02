using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;
using Symplify.BackOffice.Application.Features.Reviewers.Commands.Delete;
using Symplify.BackOffice.Application.Features.Reviewers.Commands.Update;
using Symplify.BackOffice.Application.Features.Reviewers.Queries.GetById;
using Symplify.BackOffice.Application.Features.Reviewers.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Reviewer, CreateReviewerCommand>().ReverseMap();
        CreateMap<Reviewer, CreatedReviewerResponse>().ReverseMap();
        CreateMap<Reviewer, UpdateReviewerCommand>().ReverseMap();
        CreateMap<Reviewer, UpdatedReviewerResponse>().ReverseMap();
        CreateMap<Reviewer, DeletedReviewerResponse>().ReverseMap();
        CreateMap<Reviewer, GetByIdReviewerResponse>().ReverseMap();
        CreateMap<Reviewer, GetListReviewerListItemDto>().ReverseMap();
        CreateMap<IPaginate<Reviewer>, GetListResponse<GetListReviewerListItemDto>>().ReverseMap();
    }
}
