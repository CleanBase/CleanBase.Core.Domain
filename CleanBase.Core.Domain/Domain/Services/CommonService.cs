using AutoMapper;
using CleanBase.Core.Data.UnitOfWorks;
using CleanBase.Core.Services.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanBase.Core.Domain.Domain.Services
{
	public abstract class CommonService
	{
		protected IUnitOfWork UnitOfWork { get; }
		protected IMapper Mapper { get; }
		protected ISmartLogger Logger { get; }
		protected IIdentityProvider IdentityProvider { get; }

		protected CommonService(ICoreProvider coreProvider, IUnitOfWork unitOfWork)
		{
			this.UnitOfWork = unitOfWork;
			this.Mapper = coreProvider.Mapper;
			this.Logger = coreProvider.Logger;
			this.IdentityProvider = coreProvider.IdentityProvider;
		}
	}
}
