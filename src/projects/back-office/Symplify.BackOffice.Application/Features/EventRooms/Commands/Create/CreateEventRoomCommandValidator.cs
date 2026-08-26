using FluentValidation;
namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.Create;
public class CreateEventRoomCommandValidator : AbstractValidator<CreateEventRoomCommand>
{
    public CreateEventRoomCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
