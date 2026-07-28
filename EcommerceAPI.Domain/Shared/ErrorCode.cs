namespace EcommerceAPI.Domain.Shared;

public enum ErrorCode
{
    None = 0,
    Ok = 200,
    Created = 201,

    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409
}