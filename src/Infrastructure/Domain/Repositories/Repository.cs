using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Domain.Dtos;
using Domain.Entities;
using Domain.IRepositories;
using Infrastructure.Domain.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Domain.Repositories
{
    public class Repository<TDto, TEntity> : IRepository<TDto>
        where TDto : DtoBase
        where TEntity : Entity
    {
        private AppDbContext UnitOfWork { get; set; }
        private readonly IMapper _mapper;

        public Repository(AppDbContext dbContext, IMapper mapper)
        {
            UnitOfWork = dbContext;
            _mapper = mapper;
        }

        private IQueryable<TEntity> Query(List<string>? includes, Dictionary<string, bool>? order = null, Expression<Func<TDto, bool>>? predicate = null)
        {
            IQueryable<TEntity> query = UnitOfWork.Set<TEntity>();

            if (predicate is not null)
                query = query.Where(_mapper.MapExpression<Expression<Func<TEntity, bool>>>(predicate));

            foreach (var include in includes ?? [])
                query = query.Include(include);

            IOrderedQueryable<TEntity>? ordered = null;
            foreach (var (property, ascending) in order ?? [])
            {
                ordered = (ordered, ascending) switch
                {
                    (null, true) => query.OrderBy(e => EF.Property<object>(e, property)),
                    (null, false) => query.OrderByDescending(e => EF.Property<object>(e, property)),
                    (_, true) => ordered.ThenBy(e => EF.Property<object>(e, property)),
                    (_, false) => ordered.ThenByDescending(e => EF.Property<object>(e, property)),
                };
            }
            return ordered ?? query;
        }

        public TDto? Get(Guid id, List<string>? includes = null)
        {
            var entity = Query(includes).FirstOrDefault(e => e.Id == id);
            return entity is null ? null : _mapper.Map<TDto>(entity);
        }

        public async Task<List<TDto>> GetByQueryAsync(List<string>? includes = null, Dictionary<string, bool>? order = null, Expression<Func<TDto, bool>>? predicate = null, CancellationToken cancellationToke = default)
        {
            var entities = await Query(includes,order,predicate).ToListAsync(cancellationToke);
            return _mapper.Map<List<TDto>>(entities);
        }

        public async Task<List<TDto>> GetAllAsync(List<string>? includes = null, Dictionary<string, bool>? order = null, CancellationToken cancellationToken = default)
        {
            var entities = await Query(includes, order).ToListAsync(cancellationToken);
            return _mapper.Map<List<TDto>>(entities);
        }

        public async Task<TDto?> GetAsync(Guid id, List<string>? includes = null, CancellationToken cancellationToken = default)
        {
            var entity = await Query(includes).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            return entity is null ? null : _mapper.Map<TDto>(entity);
        }

        public async Task AddAsync(TDto item, CancellationToken cancellationToken = default)
        {
            await UnitOfWork.Set<TEntity>().AddAsync(_mapper.Map<TEntity>(item), cancellationToken);
        }

        public async Task UpdateAsync(TDto item, CancellationToken cancellationToken = default)
        {
            var itemToUpdate = await UnitOfWork.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == item.Id, cancellationToken)
                ?? throw new ArgumentException("Item to update must be valid and present in database");
            UnitOfWork.Entry(itemToUpdate).CurrentValues.SetValues(_mapper.Map<TEntity>(item));
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await UnitOfWork.Set<TEntity>().FindAsync([id], cancellationToken)
                ?? throw new ArgumentException("Item to delete must be valid and present in database");
            UnitOfWork.Set<TEntity>().Remove(entity);
        }

        public async Task<int> CommitChangesAsync(CancellationToken cancellationToken = default)
        {
            return await UnitOfWork.SaveChangesAsync(cancellationToken);
        }

    }
}
