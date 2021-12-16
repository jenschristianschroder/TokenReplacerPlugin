using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using System.IO;
using System.Runtime.Serialization.Json;

namespace TokenReplacerPlugin
{
    public class ReplaceTokenWithValue : IPlugin
    {
        private PluginConfiguration config;
        public ReplaceTokenWithValue(string unsecure)
        {
            config = JsonHelper.Deserialize<PluginConfiguration>(unsecure);
        }
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                try
                {
                    // Plug-in business logic goes here.  
                    foreach(string field in config.Fields)
                    {
                        if(entity.Attributes.Contains(field))
                        {
                            string fieldValue = entity[field].ToString();
                            entity[field] = fieldValue.Replace(config.Token, config.Value);
                        }
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in CreateDispensationPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("CreateDispensationPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }

    public class PluginConfiguration
    {
        public string Token { get; set; }
        public string Value { get; set; }
        public List<string> Fields { get; set; }
    }

    public static class JsonHelper
    {
        public static T Deserialize<T>(string json)
        {
            var instance = Activator.CreateInstance<T>();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(instance.GetType());
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}
