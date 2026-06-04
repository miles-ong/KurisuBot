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

        InvalidVariantTitle,
        IdenticalVariantTitle,
        IdenticalVariantRarity,
        CannotFindVariant,
        VariantAlreadyExists
    }
}
