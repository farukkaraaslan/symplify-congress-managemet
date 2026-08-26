namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class UserManagementResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Sidebar", "BackOffice.Sidebar.SystemManagement", "Sistem Yönetimi", "System Management"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Users", "Kullanıcılar", "Users"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Roles", "Roller", "Roles"),

        new("BackOffice.Users", "BackOffice.Users.Messages.Created", "Kullanıcı oluşturuldu.", "User created successfully."),
        new("BackOffice.Users", "BackOffice.Users.Messages.Updated", "Kullanıcı güncellendi.", "User updated successfully."),
        new("BackOffice.Users", "BackOffice.Users.Messages.PasswordReset", "Kullanıcı şifresi sıfırlandı.", "User password reset successfully."),
        new("BackOffice.Users", "BackOffice.Users.Messages.RolesUpdated", "Kullanıcı rolleri güncellendi.", "User roles updated successfully."),
        new("BackOffice.Users", "BackOffice.Users.Messages.ClaimsUpdated", "Kullanıcı yetkileri güncellendi.", "User claims updated successfully."),
        new("BackOffice.Users", "BackOffice.Users.Messages.BlacklistUpdated", "Kara liste durumu güncellendi.", "Blacklist status updated successfully."),

        new("BackOffice.Roles", "BackOffice.Roles.Messages.Created", "Rol oluşturuldu.", "Role created successfully."),
        new("BackOffice.Roles", "BackOffice.Roles.Messages.Updated", "Rol güncellendi.", "Role updated successfully."),
        new("BackOffice.Roles", "BackOffice.Roles.Messages.ClaimsUpdated", "Rol yetkileri güncellendi.", "Role claims updated successfully."),
        new("BackOffice.Roles", "BackOffice.Roles.Messages.Deleted", "Rol pasife alındı.", "Role deactivated successfully."),

        // Added by localization audit: statically used keys that previously had no seed definition.
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleAssignedToUserCannotBeDeleted", "Kullanıcılara atanmış bir rol silinemez.", "A role assigned to users cannot be deleted."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleCreateFailed", "Rol oluşturulamadı.", "The role could not be created."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleDeleteFailed", "Rol silinemedi.", "The role could not be deleted."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleNameAlreadyExists", "Bu rol adı zaten kullanılıyor.", "This role name is already in use."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleNameRequired", "Rol adı zorunludur.", "Role name is required."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleNameTakenByAnother", "Bu rol adı başka bir rol tarafından kullanılıyor.", "This role name is used by another role."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleNotFound", "Rol bulunamadı.", "The role was not found."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RolePermissionsUpdateFailed", "Rol izinleri güncellenemedi.", "Role permissions could not be updated."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.RoleUpdateFailed", "Rol güncellenemedi.", "The role could not be updated."),
        new("BackOffice.Roles", "BackOffice.Roles.Business.SuperAdminRoleCannotBeDeleted", "SuperAdmin rolü silinemez.", "The SuperAdmin role cannot be deleted."),
        new("BackOffice.Users", "BackOffice.Users.Business.BlacklistStatusUpdateFailed", "Kullanıcının engel durumu güncellenemedi.", "The user's blacklist status could not be updated."),
        new("BackOffice.Users", "BackOffice.Users.Business.DefaultCongressMustBelongToOrganization", "Varsayılan kongre seçilen organizasyona ait olmalıdır.", "The default congress must belong to the selected organization."),
        new("BackOffice.Users", "BackOffice.Users.Business.DefaultCongressNotFound", "Varsayılan kongre bulunamadı.", "The default congress was not found."),
        new("BackOffice.Users", "BackOffice.Users.Business.EmailAlreadyRegistered", "Bu e-posta adresi zaten kayıtlı.", "This email address is already registered."),
        new("BackOffice.Users", "BackOffice.Users.Business.EmailTakenByAnotherUser", "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.", "This email address is used by another user."),
        new("BackOffice.Users", "BackOffice.Users.Business.PasswordResetFailed", "Kullanıcı şifresi sıfırlanamadı.", "The user's password could not be reset."),
        new("BackOffice.Users", "BackOffice.Users.Business.PasswordResetRateLimitExceeded", "Çok fazla şifre sıfırlama isteği gönderildi. Lütfen daha sonra tekrar deneyin.", "Too many password reset requests were made. Please try again later."),
        new("BackOffice.Users", "BackOffice.Users.Business.RoleNotFound", "Seçilen rol bulunamadı.", "The selected role was not found."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserCreateFailed", "Kullanıcı oluşturulamadı.", "The user could not be created."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserDeleteFailed", "Kullanıcı silinemedi.", "The user could not be deleted."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserNotFound", "Kullanıcı bulunamadı.", "The user was not found."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserPermissionsUpdateFailed", "Kullanıcı izinleri güncellenemedi.", "User permissions could not be updated."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserRolesUpdateFailed", "Kullanıcı rolleri güncellenemedi.", "User roles could not be updated."),
        new("BackOffice.Users", "BackOffice.Users.Business.UserUpdateFailed", "Kullanıcı güncellenemedi.", "The user could not be updated."),
    };
}
