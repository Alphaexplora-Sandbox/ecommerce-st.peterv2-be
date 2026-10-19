namespace EcommerceStpeterv2Backend.Domain;

public record User(
    string Id,
    string Email,
    string PasswordHash,
    string FullName,
    string PhoneNumber,
    string Role,
    DateTime CreatedAt
);