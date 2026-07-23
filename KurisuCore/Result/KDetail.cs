namespace KurisuCore.Result
{
    public enum KDetail
    {
        Completed,
        DatabaseFailure,
        DuplicateEntry,
        EntryNotFound,
        IdenticalInput,

        AnimeNotFound,
        CharacterNotFound,
        VariantNotFound,
        PackNotFound,
        CardNotFound,
        UserNotFound,
        CharacterAnimeLinkNotFound,
        VariantPackLinkNotFound,

        InvalidAnimeTitle,
        InvalidAnimeDescription,
        
        InvalidCharacterName,

        InvalidVariantAnime,
        InvalidVariantCharacter,
        InvalidVariantRarity,
        InvalidVariantTitle,
        InvalidVariantArtist,

        InvalidPackName,
        InvalidPackPrice,
        InvalidPackDiscount,
        InvalidPackFixedCardCount,
        InvalidPackWeights,

        UserMaxCardsReached,
    }
}
