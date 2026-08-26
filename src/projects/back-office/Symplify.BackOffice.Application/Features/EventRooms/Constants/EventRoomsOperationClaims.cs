namespace Symplify.BackOffice.Application.Features.EventRooms.Constants;
public static class EventRoomsOperationClaims
{
    private const string Section = "EventRooms";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}
