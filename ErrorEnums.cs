namespace KurisuBot
{
    public enum KurisuResult
    {
        Completed,
        DatabaseFailure,
        CannotFindInCache,
        CacheEmpty,

        CannotFindUser,
        UserAlreadyContainsCard,
        UserDoesNotContainCard,
        UserMaxCardsReached,
        UserCardsEmpty,

        InvalidAnimeTitle,
        NoSimilarAnimeTitles,
        CannotFindAnime,
        AnimeAlreadyContainsCharacter,
        AnimeDoesNotContainCharacter,

        InvalidCharacterName,
        NoSimilarCharacterNames,
        CannotFindCharacter,
        CharacterAlreadyContainsAnime,
        CharacterDoesNotContainAnime,
        CharacterAlreadyContainsVariant,
        CharacterDoesNotContainVariant,
        CharacterCannotBeOrphaned,
        CharacterIsNotFromAnime,
        DeletingCharacter,

        InvalidVariantTitle,
        CannotFindVariant,
        VariantCharacterAlreadyMatches,
        VariantAnimeAlreadyMatches,
        VariantRarityAlreadyMatches,
        VariantTitleAlreadyMatches
    }

    public readonly struct Result<T>
    {
        public bool Success { get; }
        public T? _value { get; }
        public KurisuResult? _error { get; }

        public T Value => Success ? _value! : throw new InvalidOperationException("[ErrorEnums] Cannot access Value when Success is false");
        public KurisuResult Error => !Success ? _error!.Value : throw new InvalidOperationException("[ErrorEnums] Cannot access Error when Success is true");

        private Result(T value)
        {
            Success = true;
            _value = value;
            _error = null;
        }

        private Result(KurisuResult error)
        {
            Success = false;
            _value = default;
            _error = error;
        }

        public static Result<T> Ok(T value) => new(value);

        public static Result<T> Fail(KurisuResult error) => new(error);
    }
}
