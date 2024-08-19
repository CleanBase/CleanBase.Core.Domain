using CleanBase.Core.Services.Core.Generic;
using CleanBase.Core.Entities.Base;
using CleanBase.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanBase.Core.Entities;
using CleanBase.Core.ViewModels.Request;
using CleanBase.Core.Data.Repositories;
using CleanBase.Core.Data.UnitOfWorks;
using CleanBase.Core.Services.Core.Base;
using System.Diagnostics;
using CleanBase.Core.Domain.Generic;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using CleanBase.Core.Services.Batchs;

namespace CleanBase.Core.Domain.Domain.Services.GenericBase
{
    public abstract class ServiceBaseCore<T, TRequest, TResponse, TGetAllRequest, TSummary> : 
        CommonService, 
        IServiceBase<T, TRequest, TResponse, TGetAllRequest, TSummary>
        where T : class, IEntityKey, new()
        where TRequest : IKeyObject
        where TGetAllRequest : GetAllRequest
        where TSummary : class, IEntityKey, new()
    {
        private readonly string _entityTypeName;
        private readonly string _serviceTypeName;
        protected Dictionary<string, string> SortFieldsMapping { get; } = new();

        public bool EnableTrackingLog { get; set; } = true;
        protected IRepository<T> Repository { get; }
        protected int PageSizeMax { get; set; } = 100;
        protected int PageSizeMin { get; set; } = 1;

        protected virtual Expression<Func<T, bool>> GetFilterExpressionForGetAllInternal(TGetAllRequest request) => null;
		protected virtual IQueryable<T> ApplyGetAllOperator(IQueryable<T> query) => query;

        protected virtual IQueryable<T> ApplyGetByIdOperator(IQueryable<T> query) => null;
		public ServiceBaseCore(ICoreProvider coreProvider, IUnitOfWork unitOfWork)
            : base(coreProvider, unitOfWork)
        {
            _serviceTypeName = GetType().Name;
            _entityTypeName = typeof(T).Name;
            Repository = unitOfWork.GetRepositoryByEntityType<T>();
            ConfigSortFieldsMapping();
            Init();
        }

        protected virtual void Init() { }

        private Stopwatch BeginTrack()
        {
            return EnableTrackingLog ? Stopwatch.StartNew() : null;
        }


		private void BuildAndTrackMessage(params object[] parts)
		{
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(50, 3);

			handler.AppendLiteral("[");
			handler.AppendFormatted(_entityTypeName);
			handler.AppendLiteral("] ");

			if (EnableTrackingLog)
			{
				Logger.Information(handler.BuildMessage(parts));
			}
		}

		public virtual async Task<T> SaveAsync(TRequest request)
        {
            var sw = BeginTrack();

            var entity = string.IsNullOrEmpty(request.Id.ToString())
                ? default
                : await Repository.FindAsync(request.Id);

            if (entity == null)
            {
                request.Id ??= Guid.NewGuid();
                entity = await MapEntityForInsert(request);
                entity = await Repository.AddAsync(entity);

                BuildAndTrackMessage(
                    "Process save add new started at ", DateTime.Now,
                    " and took ", sw?.ElapsedMilliseconds,
                    " milliseconds to complete.");
            }
            else
            {
                await MapEntityForUpdate(request, entity);
                Repository.Update(entity, false);

				BuildAndTrackMessage(
	                "Process save update started at ", DateTime.Now,
	                " and took ", sw?.ElapsedMilliseconds,
	                " milliseconds to complete.");
			}

            await UnitOfWork.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> SaveAsync(T entity)
        {
            var sw = BeginTrack();

			var existingEntity = entity.Id == Guid.Empty
                ? default
                : await Repository.FindAsync(entity.Id);

            if (existingEntity == null)
            {
				entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
				entity = await Repository.AddAsync(entity);

				BuildAndTrackMessage(
	                "Process save add new started at ", DateTime.Now,
	                " and took ", sw?.ElapsedMilliseconds,
	                " milliseconds to complete.");
			}
            else
            {
                await MapEntityForUpdate(entity, existingEntity);
                Repository.Update(existingEntity, false);

				BuildAndTrackMessage(
	                "Process save update started at ", DateTime.Now,
	                " and took ", sw?.ElapsedMilliseconds,
	                " milliseconds to complete.");
			}

            await UnitOfWork.SaveChangesAsync();
            return entity;
        }

		protected virtual Task MapEntityForUpdate(TRequest fromRequest, T toEntity)
		{
			Mapper.Map(fromRequest, toEntity);
			return Task.CompletedTask;
		}

		protected virtual Task MapEntityForUpdate(T fromEntity, T toEntity)
        {
            Mapper.Map(fromEntity, toEntity);
            return Task.CompletedTask;
        }

        protected virtual Task<T> MapEntityForInsert(TRequest request)
        {
            return Task.FromResult(Mapper.Map<T>(request));
        }

        public virtual IQueryable<T> GetAll(TGetAllRequest request)
        {
            return GetAll(request, PageSizeMax);
        }

        public virtual IQueryable<T> GetAll(TGetAllRequest request, int pageSizeMax)
        {
            var sw = BeginTrack();

            var query = Repository.GetAll()
                .Where(GetFilterExpressionForGetAll(request));

            request.SortField ??= "CreatedDate";
            request.Asc ??= false;

            if (SortFieldsMapping.TryGetValue(request.SortField, out var mappedField))
            {
                request.SortField = mappedField;
            }

            query = query.OrderDynamic(request.SortField, request.Asc.Value);
            request.PageSize = Math.Clamp(request.PageSize, PageSizeMin, pageSizeMax);

            var skip = request.Skip ?? (request.PageIndex.GetValueOrDefault() - 1) * request.PageSize;
            skip = Math.Max(0, skip);

            var result = ApplyGetAllOperator(query)
                .Skip(skip)
                .Take(request.PageSize);

			BuildAndTrackMessage(
	            "Process GetAll started at ", DateTime.Now,
	            " and took ", sw?.ElapsedMilliseconds,
	            " milliseconds to complete.");

			return result;
        }

        public virtual async Task<ListResult<T>> ListAsync(TGetAllRequest request)
        {
            var sw = BeginTrack();

            var query = Repository.GetAll()
                .Where(GetFilterExpressionForGetAll(request));

            var count = await query.CountAsync();

            request.SortField ??= "CreatedDate";
            request.Asc ??= false;

            if (SortFieldsMapping.TryGetValue(request.SortField, out var mappedField))
            {
                request.SortField = mappedField;
            }

            query = query.OrderDynamic(request.SortField, request.Asc.Value);
            request.PageSize = Math.Clamp(request.PageSize, PageSizeMin, PageSizeMax);

            var skip = request.Skip ?? (request.PageIndex.GetValueOrDefault() - 1) * request.PageSize;
            skip = Math.Max(0, skip);

            var result = new ListResult<T>(
                await ApplyGetAllOperator(query)
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync(),
                count)
            {
                Skiped = skip,
                PageSize = request.PageSize
            };

			BuildAndTrackMessage(
	            "Process List started at ", DateTime.Now,
	            " and took ", sw?.ElapsedMilliseconds,
	            " milliseconds to complete.");

			return result;
        }

        protected virtual void ConfigSortFieldsMapping() { }

        protected virtual Expression<Func<T, bool>> GetFilterExpressionForGetAll(TGetAllRequest request)
        {
            return GetFilterExpressionForGetAllInternal(request) ?? (_ => true);
        }

       

        public virtual IQueryable<TSummary> GetAllSummary(TGetAllRequest request, Expression<Func<T, TSummary>> selectExpression)
        {
            return GetAll(request).Select(selectExpression);
        }

        public virtual IQueryable<TSummary> GetAllSummary(TGetAllRequest request)
        {
            return GetAllSummary(request, t => new TSummary());
        }

		public virtual async Task<bool> HardDeleteAsync(Guid id)
		{
			bool isDeleted = await Repository.HardDeleteAsync(id);

			if (isDeleted)
			{
				await UnitOfWork.SaveChangesAsync();
			}

			return isDeleted;
		}

        public virtual async Task<bool> SoftDeleteAsync(Guid id)
        {
            bool isDeleted = await Repository.DeleteAsync(id);

            if(isDeleted)
            {
                await UnitOfWork.SaveChangesAsync();
            }

            return isDeleted;
        }

		public virtual async Task<T> GetByIdAsync(params object[] ids)
		{
			if (ids == null || ids.Length == 0)
				return default;

			if (!(ids.FirstOrDefault() is Guid id))
				return default;

			IQueryable<T> query = this.ApplyGetByIdOperator(this.Repository.GetAll());

			if (query == null)
				return await this.Repository.FindAsync(id);

			return await query.FirstOrDefaultAsync(p => p.Id == id);
		}


		public virtual async Task UpsertAsync<T2, TKey>(
	        IEnumerable<T2> source,
	        Func<T2, TKey> keySelector,
	        Func<T, TKey> sourceKeySelector,
	        Func<IQueryable<T>, List<TKey>, IQueryable<T>> getExistingEntities,
	        Action<T2, T>? onAdd = null,
	        Action<T2, T>? onUpdate = null,
	        bool allowAdd = true,
	        bool allowUpdate = true,
	        bool allowDelete = false) 
			where T2 : class
		{
			// Fetch keys and entities from the source
			var keys = source.Select(keySelector).ToHashSet();
			var sourceDict = source.ToDictionary(keySelector);

			// Get existing entities from the repository
			var existingEntities = await getExistingEntities(this.Repository.GetAll(), keys.ToList()).ToListAsync();
			var existingKeys = existingEntities.Select(sourceKeySelector).ToHashSet();

			// Add new entities
			if (allowAdd)
			{
				var newKeys = keys.Except(existingKeys).ToList();
				if (newKeys.Count > 0)
				{
					BuildAndTrackMessage(
	                    "Adding",newKeys.Count,
	                    "new entities.");

					await ProcessEntitiesAsync(newKeys, key =>
					{
						var entity = this.Mapper.Map<T>(sourceDict[key]);
						onAdd?.Invoke(sourceDict[key], entity);
						this.Repository.Add(entity, false);
					});
				}
			}

			// Update existing entities
			if (allowUpdate)
			{
				var updateKeys = keys.Intersect(existingKeys).ToList();
				if (updateKeys.Count > 0)
				{
					BuildAndTrackMessage(
	                    "Updating", updateKeys.Count,
	                    "entities.");

					await ProcessEntitiesAsync(updateKeys, key =>
					{
						var sourceEntity = sourceDict[key];
						var existingEntity = existingEntities.First(e => sourceKeySelector(e).Equals(key));
						this.Mapper.Map(sourceEntity, existingEntity);
						onUpdate?.Invoke(sourceEntity, existingEntity);
						this.Repository.Update(existingEntity, false);
					});
				}
			}

			// Delete obsolete entities
			if (allowDelete)
			{
				var obsoleteKeys = existingKeys.Except(keys).ToList();
				if (obsoleteKeys.Count > 0)
				{
					await ProcessEntitiesAsync(obsoleteKeys, key =>
					{
						var entity = existingEntities.First(e => sourceKeySelector(e).Equals(key));
						this.Repository.Delete(entity, false);
					});
				}
			}

			await this.UnitOfWork.SaveChangesAsync();
		}

		// Helper method to process entities asynchronously
		private async Task ProcessEntitiesAsync<TKey>(IEnumerable<TKey> keys, Action<TKey> process)
		{
			var tasks = keys.Select(key => Task.Run(() => process(key)));
			await Task.WhenAll(tasks);
		}

		public virtual async Task UpsertBatchAsync<T2, TKey>(
			IEnumerable<T2> source,
			Func<T2, TKey> keySelector,
			Func<T, TKey> sourceKeySelector,
			Func<IQueryable<T>, List<TKey>, IQueryable<T>> existingEntitiesPredicate,
			Action<T2, T> onAdd = null,
			Action<T2, T> onUpdate = null,
			bool allowAdd = true,
			bool allowUpdate = true,
			bool allowDelete = false)
			where T2 : class
		{
			var sourceDict = source.ToDictionary(keySelector);

			// Định nghĩa actionAsync
			Func<int, int, Task<IEnumerable<T>>> actionAsync = async (index, pageSize) =>
			{
				var keys = sourceDict.Keys.Skip((index - 1) * pageSize).Take(pageSize).ToList();
				return await existingEntitiesPredicate(this.Repository.GetAll(), keys).ToListAsync();
			};

			// Định nghĩa operation
			Func<IEnumerable<T>, Task> operation = async entities =>
			{
				var existingKeys = entities.Select(sourceKeySelector).ToHashSet();
				var keys = sourceDict.Keys.ToHashSet();

				if (allowAdd)
				{
					var newKeys = keys.Except(existingKeys).ToList();
					if (newKeys.Any())
					{
						this.Logger.Information($"newKeys: {newKeys.Count}");

						var newEntities = newKeys.Select(key =>
						{
							var entity = this.Mapper.Map<T>(sourceDict[key]);
							entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
							onAdd?.Invoke(sourceDict[key], entity);
							return entity;
						}).ToList();

						await this.Repository.BatchAddAsync(newEntities);
					}
				}

				if (allowUpdate)
				{
					var updateKeys = keys.Intersect(existingKeys).ToList();
					if (updateKeys.Any())
					{
						this.Logger.Information($"updateKeys: {updateKeys.Count}");

						var updateEntities = updateKeys.Select(key =>
						{
							var sourceEntity = sourceDict[key];
							var existingEntity = entities.First(e => sourceKeySelector(e).Equals(key));
							this.Mapper.Map(sourceEntity, existingEntity);
							onUpdate?.Invoke(sourceEntity, existingEntity);
							return existingEntity;
						}).ToList();

						await this.Repository.BatchUpdateAsync(updateEntities);
					}
				}

				if (allowDelete)
				{
					var deleteKeys = existingKeys.Except(keys).ToList();
					if (deleteKeys.Any())
					{
						this.Logger.Information($"deleteKeys: {deleteKeys.Count}");

						var deleteEntities = deleteKeys.Select(key => entities.First(e => sourceKeySelector(e).Equals(key))).ToList();
						foreach (var entity in deleteEntities)
						{
							await this.Repository.DeleteAsync(entity, false); // Đảm bảo DeleteAsync có sẵn
						}
					}
				}
			};

			// Tạo đối tượng BatchOperation và thực hiện
			var batchOperation = new BatchOperation<T>(actionAsync, operation);
			await batchOperation.RunBatchOperationAsync();

			await this.UnitOfWork.SaveChangesAsync();
		}

	}
}
