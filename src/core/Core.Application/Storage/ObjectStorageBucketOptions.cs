namespace Core.Application.Storage;

public sealed class ObjectStorageBucketOptions
{
    public string CongressDocuments { get; set; } = "symplify-congress-documents";

    public string CongressImages { get; set; } = "symplify-congress-images";

    public string Submissions { get; set; } = "symplify-submissions";
}
