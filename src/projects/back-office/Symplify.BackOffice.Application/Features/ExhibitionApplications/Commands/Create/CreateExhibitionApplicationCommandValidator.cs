using FluentValidation;
using Symplify.BackOffice.Application.Features.ExhibitionApplications.Constants;

namespace Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;

public sealed class CreateExhibitionApplicationCommandValidator : AbstractValidator<CreateExhibitionApplicationCommand>
{
    public CreateExhibitionApplicationCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.CongressRequired);

        RuleFor(command => command.SubmissionTypeId)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.TypeRequired);

        RuleFor(command => command.WorkName)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.WorkNameRequired)
            .MaximumLength(300)
            .WithMessage(ExhibitionApplicationsMessages.WorkNameMaxLength);

        RuleFor(command => command.Dimensions)
            .MaximumLength(200)
            .WithMessage(ExhibitionApplicationsMessages.DimensionsMaxLength);

        RuleFor(command => command.Technique)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.TechniqueRequired)
            .MaximumLength(250)
            .WithMessage(ExhibitionApplicationsMessages.TechniqueMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(4000)
            .WithMessage(ExhibitionApplicationsMessages.DescriptionMaxLength);

        RuleFor(command => command.Address)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.AddressRequired)
            .MaximumLength(1000)
            .WithMessage(ExhibitionApplicationsMessages.AddressMaxLength);

        RuleFor(command => command.File)
            .NotNull()
            .WithMessage(ExhibitionApplicationsMessages.FileRequired);

        RuleFor(command => command.Authors)
            .NotEmpty()
            .WithMessage(ExhibitionApplicationsMessages.AuthorsRequired);
    }
}
