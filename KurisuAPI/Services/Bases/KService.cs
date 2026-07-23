using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services.Bases
{
    public abstract class KService
    {
        protected abstract string ModelName { get; }
        protected abstract string ConsoleTag { get; }
        protected abstract KDetail NotFoundDetail { get; }

        protected async Task<KDetail> TryInsertAsync(
            Func<Task> insertAsync, 
            Action onSuccess,
            string context)
        {
            try
            {
                await insertAsync();
                onSuccess();

                return KDetail.Completed;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                Console.WriteLine($"{ConsoleTag} Duplicate {ModelName} {context}: {ex.Message}");

                return KDetail.DuplicateEntry;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConsoleTag} Failed to add {ModelName} {context}: {ex.Message}");

                return KDetail.DatabaseFailure;
            }
        }

        protected async Task<KDetail> TryUpdateAsync(
            KDetail mutationDetail,
            Func<Task> updateAsync,
            Action revert,
            string context)
        {
            if (mutationDetail != KDetail.Completed) return mutationDetail;

            try
            {
                await updateAsync();

                return KDetail.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                revert();
                Console.WriteLine($"{ConsoleTag} {ModelName} {context} not found: {ex.Message}");

                return NotFoundDetail;
            }
            catch (Exception ex)
            {
                revert();
                Console.WriteLine($"{ConsoleTag} Failed to update {ModelName} {context}: {ex.Message}");

                return KDetail.DatabaseFailure;
            }
        }

        protected async Task<KDetail> TryDeleteAsync(
            Func<Task> deleteAsync,
            Action onSuccess,
            string context)
        {
            try
            {
                await deleteAsync();
                onSuccess();

                return KDetail.Completed;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"{ConsoleTag} {ModelName} {context} not found: {ex.Message}");

                return NotFoundDetail;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConsoleTag} Failed to delete {ModelName} {context}: {ex.Message}");

                return KDetail.DatabaseFailure;
            }
        }
    }
}
