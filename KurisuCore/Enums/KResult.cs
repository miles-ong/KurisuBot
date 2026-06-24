namespace KurisuCore.Enums
{
    public enum KResult
    {
        Completed,
        DatabaseFailure,

        InvalidUsername,
        CannotFindUser,
        UserAlreadyContainsCard,
        UserDoesNotContainCard,
        UserMaxCardsReached,
        UserCardsEmpty,

        InvalidAnimeTitle,
        IdenticalAnimeTitle,
        InvalidAnimeDescription,
        IdenticalAnimeDescription,
        CannotFindAnime,
        AnimeAlreadyExists,

        InvalidCharacterName,
        IdenticalCharacterName,
        CannotFindCharacter,
        CharacterAlreadyExists,

        CannotFindCharacterAnimeLink,
        CharacterAnimeLinkAlreadyExists,
        CharacterAnimeLinkDoesNotExist,

        IdenticalVariantAnime,
        IdenticalVariantRarity,
        InvalidVariantTitle,
        IdenticalVariantTitle,
        CannotFindVariant,
        VariantAlreadyExists,

        InvalidPackName,
        IdenticalPackName,
        InvalidPackPrice,
        IdenticalPackPrice,
        InvalidPackWeights,
        PackIsAlreadyEnabled,
        PackIsAlreadyDisabled
    }
}
