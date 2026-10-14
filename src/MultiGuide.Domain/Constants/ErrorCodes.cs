namespace MultiGuide.Domain.Constants;

public static class ErrorCodes
{
    // 401
    public const string PassMissing = "PASS_MISSING";
    public const string AuthRequired = "AUTH_REQUIRED";

    // 402
    public const string PaymentFailed = "PAYMENT_FAILED";

    // 403
    public const string PassInvalid = "PASS_INVALID";
    public const string PassExpired = "PASS_EXPIRED";
    public const string PassAreaMismatch = "PASS_AREA_MISMATCH";
    public const string Forbidden = "FORBIDDEN";

    // 404
    public const string AreaNotFound = "AREA_NOT_FOUND";
    public const string PoiContentNotFound = "POI_CONTENT_NOT_FOUND";
    public const string NotFound = "NOT_FOUND";

    // 409
    public const string DuplicateRequest = "DUPLICATE_REQUEST";
    public const string TranslationExists = "TRANSLATION_EXISTS";

    // 422
    public const string ValidationError = "VALIDATION_ERROR";

    // 429
    public const string RateLimited = "RATE_LIMITED";

    // 500
    public const string StorageError = "STORAGE_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}
