using CleanBase.Core.Data.UnitOfWorks;
using CleanBase.Core.Entities;
using CleanBase.Core.Entities.Base;
using CleanBase.Core.Services.Core.Base;
using CleanBase.Core.ViewModels.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CleanBase.Core.Domain.Domain.Services.GenericBase
{
	public abstract class ServiceBase<T, TRequest, TResponse, TGetAllRequest, TSummary> :
		ServiceBaseCore<T, TRequest, TResponse, TGetAllRequest, TSummary>
		where T : class, IEntityKeyName, new()
		where TRequest : IKeyObject
		where TGetAllRequest : GetAllRequest
		where TSummary : class, IEntityKeyName, new()
	{
		public ServiceBase(ICoreProvider coreProvider, IUnitOfWork unitOfWork)
		  : base(coreProvider, unitOfWork)
		{
		}

		public override IQueryable<TSummary> GetAllSummary(TGetAllRequest request)
		{
			// Create a lambda expression for converting T to TSummary
			var parameter = Expression.Parameter(typeof(T), "x");
			var newSummary = Expression.New(typeof(TSummary));

			// Bind properties of TSummary to values from T
			var bindings = typeof(TSummary).GetProperties()
				.Select(p => Expression.Bind(p, Expression.Property(parameter, typeof(T).GetProperty(p.Name))));

			var memberInit = Expression.MemberInit(newSummary, bindings);
			var lambda = Expression.Lambda<Func<T, TSummary>>(memberInit, parameter);

			// Call the base method with the constructed lambda expression
			return this.GetAllSummary(request, lambda);
		}
	}
}
