using FluentValidation;
namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.Update;
public class UpdateEventRoomCommandValidator : AbstractValidator<UpdateEventRoomCommand>
{
    public UpdateEventRoomCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
