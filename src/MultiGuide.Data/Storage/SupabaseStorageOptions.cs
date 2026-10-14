namespace MultiGuide.Data.Storage;

public class SupabaseStorageOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string AudioBucket { get; set; } = "audio";
}
