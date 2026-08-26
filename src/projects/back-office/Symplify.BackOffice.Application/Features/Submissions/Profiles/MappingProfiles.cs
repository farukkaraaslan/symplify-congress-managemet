using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Submissions.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<Submission, CreatedSubmissionResponse>().ReverseMap();

        CreateMap<Submission, UpdateSubmissionCommand>()
            .ForMember(destination => destination.Authors, options => options.Ignore());

        CreateMap<UpdateSubmissionCommand, Submission>()
            .ForMember(destination => destination.CongressId, options => options.Ignore())
            .ForMember(destination => destination.CreatedByUserId, options => options.Ignore())
            .ForMember(destination => destination.PaymentStatusId, options => options.Ignore())
            .ForMember(destination => destination.TransactionStatusId, options => options.Ignore())
            .ForMember(destination => destination.SubmissionNumber, options => options.Ignore())
            .ForMember(destination => destination.IsSubmitted, options => options.Ignore())
            .ForMember(destination => destination.SubmittedAt, options => options.Ignore())
            .ForMember(destination => destination.Authors, options => options.Ignore())
            .ForMember(destination => destination.Reviewers, options => options.Ignore())
            .ForMember(destination => destination.Evaluations, options => options.Ignore())
            .ForMember(destination => destination.Histories, options => options.Ignore())
            .ForMember(destination => destination.Files, options => options.Ignore())
            .ForMember(destination => destination.AcceptanceLetters, options => options.Ignore());

        CreateMap<Submission, UpdatedSubmissionResponse>().ReverseMap();
        CreateMap<Submission, DeletedSubmissionResponse>().ReverseMap();
        CreateMap<Submission, GetByIdSubmissionResponse>()
            .ForMember(destination => destination.Authors, options => options.Ignore())
            .ReverseMap()
            .ForMember(destination => destination.Authors, options => options.Ignore());
        CreateMap<Submission, GetListSubmissionListItemDto>().ReverseMap();
        CreateMap<IPaginate<Submission>, GetListResponse<GetListSubmissionListItemDto>>().ReverseMap();
    }
}
