using CleanBase.Core.Services.External.MessagesBus;
using CleanBase.Core.Services.External.MessagesBus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanBase.Core.Domain.Services.External.MessagesBus
{
	public class EmptyMessageBrokerService : IMessageBrokerService
	{
		public Task SendMessageAsync<T>(T obj) where T : IServiceBusMessage => Task.CompletedTask;

	}
}
