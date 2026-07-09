namespace Nexus.Scripts.Errors;

public static class ScriptErrorCodes
{
    public const string NameInvalid = "Script.NAME_INVALID";
    public const string DescriptionInvalid = "Script.DESCRIPTION_INVALID";
    public const string NameAlreadyExists = "Script.NAME_ALREADY_EXISTS";
    public const string ScriptNotFound = "Script.SCRIPT_NOT_FOUND";
    public const string ScopeRequired = "Script.SCOPE_REQUIRED";
    public const string ScopeInvalid = "Script.SCOPE_INVALID";
    public const string HostPatternInvalid = "Script.HOST_PATTERN_INVALID";
    public const string ChannelKeyInvalid = "Script.CHANNEL_KEY_INVALID";
    public const string ChannelNotFound = "Script.CHANNEL_NOT_FOUND";
    public const string ChannelAlreadyExists = "Script.CHANNEL_ALREADY_EXISTS";
    public const string CustomChannelNameRequired = "Script.CUSTOM_CHANNEL_NAME_REQUIRED";
    public const string ReleaseNotFound = "Script.RELEASE_NOT_FOUND";
    public const string ReleaseIdInvalid = "Script.RELEASE_ID_INVALID";
    public const string SourceCodeRequired = "Script.SOURCE_CODE_REQUIRED";
    public const string VersionInvalid = "Script.VERSION_INVALID";
    public const string VersionAlreadyExists = "Script.VERSION_ALREADY_EXISTS";
    public const string ChannelHasNoRelease = "Script.CHANNEL_HAS_NO_RELEASE";
    public const string ResolveHostOrNameRequired = "Script.RESOLVE_HOST_OR_NAME_REQUIRED";
    public const string SearchLimitInvalid = "Script.SEARCH_LIMIT_INVALID";
    public const string SearchOffsetInvalid = "Script.SEARCH_OFFSET_INVALID";
    public const string SearchKeywordTooLong = "Script.SEARCH_KEYWORD_TOO_LONG";
    public const string RequestBodyRequired = "Script.REQUEST_BODY_REQUIRED";
}
