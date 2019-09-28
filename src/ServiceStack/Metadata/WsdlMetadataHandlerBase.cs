#if !NETSTANDARD2_1

using System;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public abstract class WsdlMetadataHandlerBase : HttpAsyncTaskHandler
    {
        private readonly ILog log = LogManager.GetLogger(typeof(WsdlMetadataHandlerBase));

        protected abstract WsdlTemplateBase GetWsdlTemplate();

        public Task Execute(IRequest httpReq, IResponse httpRes)
        {
            HostContext.AppHost.AssertFeatures(Feature.Metadata);

            httpRes.ContentType = "text/xml";

            var baseUri = httpReq.GetParentBaseUrl();
            var optimizeForFlash = httpReq.QueryString["flash"] != null;
            var operations = new XsdMetadata(HostContext.Metadata, flash: optimizeForFlash);

            try
            {
                var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, httpReq.GetBaseUrl(), HostContext.Config.SoapServiceName);
                var wsdl = HostContext.AppHost.GenerateWsdl(wsdlTemplate);
                return httpRes.WriteAsync(wsdl);
            }
            catch (Exception ex)
            {
                log.Error("Autogeneration of WSDL failed.", ex);

                return httpRes.WriteAsync("Autogenerated WSDLs are not supported "
                    + (Env.IsMono ? "on Mono" : "with this configuration"));
            }
        }

        public WsdlTemplateBase GetWsdlTemplate(XsdMetadata operations, string baseUri, bool optimizeForFlash, string rawUrl, string serviceName)
        {
            var soapTypes = operations.Metadata.GetAllSoapOperationTypes();
            var xsd = new XsdGenerator
            {
                OperationTypes = soapTypes,
                OptimizeForFlash = optimizeForFlash,
            }.ToString();

            var soapFormat = GetType().GetOperationName().StartsWith("Soap11", StringComparison.OrdinalIgnoreCase)
                ? Format.Soap11 : Format.Soap12;

            var wsdlTemplate = GetWsdlTemplate();
            wsdlTemplate.Xsd = xsd;
            wsdlTemplate.ServiceName = serviceName;

            var soapTypesSet = soapTypes.ToHashSet();
            wsdlTemplate.ReplyOperationNames = operations.GetReplyOperationNames(soapFormat, soapTypesSet);
            wsdlTemplate.OneWayOperationNames = operations.GetOneWayOperationNames(soapFormat, soapTypesSet);

            if (rawUrl.ToLower().StartsWith(baseUri))
            {
                wsdlTemplate.ReplyEndpointUri = rawUrl;
                wsdlTemplate.OneWayEndpointUri = rawUrl;
            }
            else
            {
                var suffix = soapFormat == Format.Soap11 ? "soap11" : "soap12";
                wsdlTemplate.ReplyEndpointUri = baseUri + suffix;
                wsdlTemplate.OneWayEndpointUri = baseUri + suffix;
            }

            return wsdlTemplate;
        }
    }
}

#endif
