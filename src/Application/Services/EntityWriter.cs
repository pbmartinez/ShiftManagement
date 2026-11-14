using System.ComponentModel.DataAnnotations;
using System.Data;
using Application.Abstractions;
using Domain.Dtos;
using Domain.IRepositories;

namespace Application.Services
{
    public class EntityWriter<TDto>(
        IRepository<TDto> repository,
        IEnumerable<IBusinessValidator<TDto>> validators,
        IUnitOfWork unitOfWork) : IEntityWriter<TDto>
        where TDto : DtoBase
    {
        public Task<List<string>> AddAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
                item.GenerateIdentity();

            return ValidateAndSaveAsync(items, async ct =>
            {
                foreach (var item in items)
                    await repository.AddAsync(item, ct);
            }, cancellationToken);
        }

        public Task<List<string>> UpdateAsync(TDto item, CancellationToken cancellationToken = default)
        {
            return ValidateAndSaveAsync([item], ct => repository.UpdateAsync(item, ct), cancellationToken);
        }

        private async Task<List<string>> ValidateAndSaveAsync(
            IReadOnlyList<TDto> items,
            Func<CancellationToken, Task> save,
            CancellationToken cancellationToken)
        {
            var errors = ValidateDataAnnotations(items);
            if (errors.Count > 0)
                return errors;

            // Serializable: the business rules read the database and the save happens in the same unit,
            // so two requests at the same time can't both pass a rule (e.g. no overlaps) and then both save.
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var validator in validators)
                    errors.AddRange(await validator.ValidateAsync(items, ct));
                if (errors.Count > 0)
                    return errors;

                await save(ct);
                await repository.CommitChangesAsync(ct);
                return errors;
            }, IsolationLevel.Serializable, cancellationToken);
        }

        // Model binding already does this for request bodies, but imports and other callers don't go through it.
        private static List<string> ValidateDataAnnotations(IReadOnlyList<TDto> items)
        {
            var errors = new List<string>();
            foreach (var item in items)
            {
                var results = new List<ValidationResult>();
                if (Validator.TryValidateObject(item, new ValidationContext(item), results, validateAllProperties: true))
                    continue;

                // With several items, say which one failed.
                errors.AddRange(results.Select(r => items.Count == 1 ? r.ErrorMessage! : $"{item}: {r.ErrorMessage}"));
            }
            return errors;
        }
    }
}
