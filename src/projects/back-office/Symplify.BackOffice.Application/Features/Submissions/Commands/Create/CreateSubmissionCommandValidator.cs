using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Helpers;

namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Create;

public sealed class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    private const int AbstractPlainTextMaxLength = 25000;

    public CreateSubmissionCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(SubmissionValidationMessages.CongressRequired);

        RuleFor(command => command.SubmissionTypeId)
            .NotEmpty()
            .WithMessage(SubmissionValidationMessages.SubmissionTypeRequired);

        RuleFor(command => command.TopicId)
            .NotEmpty()
            .WithMessage(SubmissionValidationMessages.TopicRequired);

        RuleFor(command => command.Title)
            .NotEmpty().WithMessage(SubmissionValidationMessages.TitleRequired)
            .MaximumLength(300).WithMessage(SubmissionValidationMessages.TitleMaxLength);

        RuleFor(command => command.Keywords)
            .NotEmpty().WithMessage(SubmissionValidationMessages.KeywordsRequired)
            .MaximumLength(500).WithMessage(SubmissionValidationMessages.KeywordsMaxLength);

        RuleFor(command => command.Abstract)
            .NotEmpty().WithMessage(SubmissionValidationMessages.AbstractRequired)
            .Must(value => SubmissionHtmlTextLengthHelper.IsWithinPlainTextLimit(value, AbstractPlainTextMaxLength))
            .WithMessage(SubmissionValidationMessages.AbstractMaxLength);

        RuleFor(command => command.TitleEn)
            .MaximumLength(300).WithMessage(SubmissionValidationMessages.TitleEnMaxLength);

        RuleFor(command => command.KeywordsEn)
            .MaximumLength(500).WithMessage(SubmissionValidationMessages.KeywordsEnMaxLength);

        RuleFor(command => command.AbstractEn)
            .Must(value => SubmissionHtmlTextLengthHelper.IsWithinPlainTextLimit(value, AbstractPlainTextMaxLength))
            .WithMessage(SubmissionValidationMessages.AbstractEnMaxLength);

        RuleFor(command => command.Orcid)
            .MaximumLength(50).WithMessage(SubmissionValidationMessages.OrcidMaxLength);

        RuleFor(command => command.Authors)
            .NotEmpty()
            .WithMessage(SubmissionValidationMessages.AuthorsRequired);

        RuleFor(command => command.Authors)
            .Must(authors => authors.Any(author => author.IsCorrespondingAuthor))
            .When(command => command.Authors.Count > 0)
            .WithMessage(SubmissionValidationMessages.CorrespondingAuthorRequired);

        RuleFor(command => command.Authors)
            .Must(HaveUniqueAuthorEmails)
            .When(command => command.Authors.Count > 1)
            .WithMessage(SubmissionValidationMessages.DuplicateAuthorEmail);

        RuleForEach(command => command.Authors)
            .ChildRules(author =>
            {
                author.RuleFor(item => item.TitleId)
                    .NotEmpty()
                    .WithMessage(SubmissionValidationMessages.AuthorTitleRequired);

                author.RuleFor(item => item.FullName)
                    .NotEmpty().WithMessage(SubmissionValidationMessages.AuthorFullNameRequired)
                    .MaximumLength(250).WithMessage(SubmissionValidationMessages.AuthorFullNameMaxLength);

                author.RuleFor(item => item.Email)
                    .NotEmpty().WithMessage(SubmissionValidationMessages.AuthorEmailRequired)
                    .EmailAddress().WithMessage(SubmissionValidationMessages.AuthorEmailInvalid)
                    .MaximumLength(150).WithMessage(SubmissionValidationMessages.AuthorEmailMaxLength);

                author.RuleFor(item => item.Institution)
                    .NotEmpty().WithMessage(SubmissionValidationMessages.AuthorInstitutionRequired)
                    .MaximumLength(250).WithMessage(SubmissionValidationMessages.AuthorInstitutionMaxLength);

                author.RuleFor(item => item.Orcid)
                    .MaximumLength(50).WithMessage(SubmissionValidationMessages.AuthorOrcidMaxLength);
            });
    }

    private static bool HaveUniqueAuthorEmails(IEnumerable<SubmissionAuthorInputDto> authors)
    {
        HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);

        foreach (SubmissionAuthorInputDto author in authors)
        {
            string? email = author.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                continue;

            if (!emails.Add(email))
                return false;
        }

        return true;
    }
}
