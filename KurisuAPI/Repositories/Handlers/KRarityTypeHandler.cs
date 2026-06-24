using Dapper;
using KurisuCore.Enums;
using System.Data;

namespace KurisuAPI.Repositories.Handlers
{
    public sealed class KRarityTypeHandler : SqlMapper.TypeHandler<KRarity>
    {
        public override KRarity Parse(object value)
            => Enum.Parse<KRarity>((string)value);

        public override void SetValue(IDbDataParameter parameter, KRarity value)
            => parameter.Value = value.ToString();
    }
}
