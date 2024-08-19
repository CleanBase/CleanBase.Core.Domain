using CleanBase.Core.Data.UnitOfWorks;
using CleanBase.Core.Entities;
using CleanBase.Core.Entities.Base;
using CleanBase.Core.Services.Core.Base;
using CleanBase.Core.ViewModels.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanBase.Core.Domain.Domain.Services.GenericBase
{
	public abstract class ServiceBaseCore<T, TRequest, TResponse, TGetAllRequest> :
		ServiceBaseCore<T, TRequest, TResponse, TGetAllRequest, EntityBaseName>
		where T : class, IEntityKey, new()
		where TRequest : IKeyObject
		where TGetAllRequest : GetAllRequest
	{
		public ServiceBaseCore(ICoreProvider coreProvider, IUnitOfWork unitOfWork)
		  : base(coreProvider, unitOfWork)
		{
		}
	}
}
