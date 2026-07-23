using KurisuAPI.Caches.Bases;
using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Bases;
using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services.Bases;

    public abstract class KDataService<TId, TEntity, TData>(
        KDataRepository<TId, TData> repo, 
        IKCache<TId, TEntity> cache,
        KEntityFactory factory) 
        : KService 
        where TId : IEquatable<TId>
        where TEntity : KEntity<TId, TData>
        where TData : IKData<TId>
    {
        protected KEntityFactory Factory => factory;
        
        public async Task<TEntity?> GetByIdAsync(TId id) => await Factory.LoadAsync<TId, TEntity, TData>(id);

        public async Task<KResult<TEntity>> AddAsync(TData data)
        {
            try
            {
                var id = await repo.InsertAsync(data);
                var entity = await Factory.BuildAsync<TId, TEntity, TData>(id, data);

                return KResult<TEntity>.Ok(entity);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                Console.WriteLine($"{ConsoleTag} Duplicate {ModelName}: {ex.Message}");

                return KResult<TEntity>.Fail(KDetail.DuplicateEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConsoleTag} Failed to add {ModelName}: {ex.Message}");

                return KResult<TEntity>.Fail(KDetail.DatabaseFailure);
            }
        }

        public async Task<KDetail> DeleteAsync(TId id)
        {
            var result = TryFind(id);

            if (!result.Success) return result.Detail;

            var entity = result.Value;

            return await TryDeleteAsync(
                () => repo.DeleteAsync(id),
                () => Factory.Destroy(entity),
                entity.ToString() ?? string.Empty);
        }

        protected async Task<KDetail> SetAsync<TValue>(
            TId id,
            Func<TEntity, TValue> getValue,
            Action<TEntity, TValue> setValue,
            TValue newValue,
            string context,
            Func<TEntity, TValue, KDetail>? validate = null,
            Action<TEntity, TValue>? onPersisted = null)
        {
            var found = TryFind(id);

            if (!found.Success) return found.Detail;

            var entity = found.Value;
            var oldValue = getValue(entity);
            var validation = validate?.Invoke(entity, newValue) ?? KDetail.Completed;

            if (validation != KDetail.Completed) return validation;

            setValue(entity, newValue);

            return await TryUpdateAsync(
                KDetail.Completed,
                async () =>
                {
                    await repo.UpdateAsync(entity.Id, entity.ToData());
                    onPersisted?.Invoke(entity, oldValue);
                },
                () => setValue(entity, oldValue),
                context);
        }
        
        private KResult<TEntity> TryFind(TId id) =>
            cache.TryGet(id, out var entity) ? KResult<TEntity>.Ok(entity!) : KResult<TEntity>.Fail(NotFoundDetail); 
    }

    public abstract class KDataService<TEntity, TData>(
        KDataRepository<TData> repo,
        IKCache<TEntity> cache,
        KEntityFactory factory)
        : KDataService<int, TEntity, TData>(repo, cache, factory)
        where TEntity : KEntity<TData>
        where TData : IKData;        