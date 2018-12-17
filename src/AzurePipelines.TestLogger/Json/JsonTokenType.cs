namespace AzurePipelines.TestLogger.Json
{
    internal enum JsonTokenType
    {
        LeftCurlyBracket,   // [
        LeftSquareBracket,  // {
        RightCurlyBracket,  // ]
        RightSquareBracket, // }
        Colon,              // :
        Comma,              // ,
        Null,
        True,
        False,
        Number,
        String,
        EOF
    }
}
