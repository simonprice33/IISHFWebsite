using IISHF.Core.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Services
{
    public class DataPushService
    {
        private readonly IHubContext<DataHub> _hubContext;

        public DataPushService(IHubContext<DataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushDataUpdate(string user, string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
