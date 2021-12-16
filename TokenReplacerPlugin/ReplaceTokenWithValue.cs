using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Xrm.Sdk.Messages;

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
                        // Check field exist on target entity
                        if(entity.Attributes.Contains(field))
                        {
                            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                            // Retrieve field attributes
                            RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest
                            {
                                EntityLogicalName = entity.LogicalName,
                                LogicalName = field,
                                RetrieveAsIfPublished = true
                            };
                            RetrieveAttributeResponse attributeResponse = (RetrieveAttributeResponse)service.Execute(attributeRequest);

                            // Check attribute type is supported
                            if(attributeResponse.AttributeMetadata.AttributeType != Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Memo && attributeResponse.AttributeMetadata.AttributeType != Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.String)
                            {
                                tracingService.Trace($"TokenReplacePlugin: {field} not of support type (Memo or String). {field} is of type {attributeResponse.AttributeMetadata.AttributeType}");
                                throw new Exception($"TokenReplacePlugin: {field} not of support type (Memo or String). {field} is of type {attributeResponse.AttributeMetadata.AttributeType}");
                            }
                            
                            // Replace token with value
                            string fieldValue = entity[field].ToString();
                            fieldValue = fieldValue.Replace(config.Token, config.Value);

                            // Check if value should be trimmed in length. This only happens if value has longer length than token (token: abc, value: 12345 could potentially cause the value to exceed the MaxLength of the field)
                            if (config.TrimMaxLength)
                            {
                                int? fieldMaxLength = null;

                                // Read MaxLength property of field
                                switch (attributeResponse.AttributeMetadata.AttributeType)
                                {
                                    case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Memo:
                                        {
                                            fieldMaxLength = ((Microsoft.Xrm.Sdk.Metadata.MemoAttributeMetadata)attributeResponse.AttributeMetadata).MaxLength;
                                            break;
                                        }
                                    case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.String:
                                        {
                                            fieldMaxLength = ((Microsoft.Xrm.Sdk.Metadata.StringAttributeMetadata)attributeResponse.AttributeMetadata).MaxLength;
                                            break;
                                        }
                                }
                                
                                if(fieldMaxLength.HasValue)
                                {
                                    if(fieldValue.Length > fieldMaxLength.Value)
                                    { 
                                        // Trim length of field value
                                        fieldValue = fieldValue.Substring(0, fieldMaxLength.Value);
                                    }
                                }
                                else
                                {
                                    tracingService.Trace("TokenReplacePlugin: MaxLength null");
                                    throw new Exception("An error occured in TokenReplacerPlugin: MaxLength null");
                                }
                            }

                            // Set field value
                            entity[field] = fieldValue;
                        }
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in TokenReplacerPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("TokenReplacerPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }

    public class PluginConfiguration
    {
        public bool TrimMaxLength { get; set; }
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
