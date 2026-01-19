using DAS.DigitalEngagement.Models.Infrastructure;
using DAS.DigitalEngagement.Models.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Handlers.Configuration
{
    public class ConfigureDataModelHandler: IConfigureDataModelHandler
    {
        private readonly ILogger _logger;
        public ConfigureDataModelHandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConfigureDataModelHandler>();

        }
    }
}
