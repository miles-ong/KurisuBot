namespace KurisuAPI.Records
{
    public sealed record KCardRow (
        long Id,
        int VariantId,
        DateTime TimeObtained,
        int? BorderId,
        int? OverlayId
    );
}
