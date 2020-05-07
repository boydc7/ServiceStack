using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Desktop
{
    public class DesktopFeature : IPlugin, IPreInitPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Desktop;
        public string AccessRole { get; set; } = RoleNames.Admin;

        public List<string> ImportParams { get; } = new List<string>();
        public List<ProxyConfig> ProxyConfigs { get; set; } = new List<ProxyConfig>();

        public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new Dictionary<Type, string[]> {
            { typeof(DesktopScriptServices), new []{ "/script" } },
        };

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            appHost.Config.EmbeddedResourceBaseTypes.Add(typeof(DesktopAssets));

            appHost.ScriptContext.ScriptMethods.Add(new DesktopScripts(
                scope => {
                    if (scope.TryGetValue(ScriptConstants.Request, out var oRequest) && oRequest is IRequest req)
                    {
                        var info = req.GetHeader("X-Desktop-Info");
                        if (info != null)
                            NativeWin.SetDesktopInfo(info.FromJsv<Dictionary<string, string>>());
                        var handle = req.GetHeader("X-Window-Handle");
                        if (handle != null && long.TryParse(handle, out var lHandle))
                            return (IntPtr)lHandle;
                    }
                    return IntPtr.Zero;
                }));
        }
        
        public void Register(IAppHost appHost)
        {
            appHost.RegisterServices(ServiceRoutes);
            DesktopConfig.Instance.ImportParams.AddRange(ImportParams);
            DesktopConfig.Instance.ProxyConfigs.AddRange(ProxyConfigs);
        }
    }

    public class EvalScript : IReturn<string>
    {
        public string AuthSecret { get; set; }
        
        public string EvaluateScript { get; set; }
        public string EvaluateCode { get; set; }
        public string EvaluateLisp { get; set; }
        public string RenderScript { get; set; }
        public string RenderCode { get; set; }
        public string RenderLisp { get; set; }
        
        public string EvaluateScriptAsync { get; set; }
        public string EvaluateCodeAsync { get; set; }
        public string EvaluateLispAsync { get; set; }
        public string RenderScriptAsync { get; set; }
        public string RenderCodeAsync { get; set; }
        public string RenderLispAsync { get; set; }
    }

    [DefaultRequest(typeof(EvalScript))]
    public class DesktopScriptServices : Service
    {
        public static ILog log = LogManager.GetLogger(typeof(DesktopScriptServices));
        
        public async Task Any(EvalScript request)
        {
            var feature = HostContext.AssertPlugin<DesktopFeature>();
            RequestUtils.AssertAccessRole(base.Request, accessRole: feature.AccessRole, authSecret: request.AuthSecret);

            var appHost = HostContext.AppHost;
            string script;
            var method = ((script = request.EvaluateScript) != null
                 ? nameof(request.EvaluateScript)
                 : (script = request.EvaluateCode) != null
                     ? nameof(request.EvaluateCode)
                     : (script = request.EvaluateLisp) != null
                         ? nameof(request.EvaluateLisp)
                         : (script = request.RenderScript) != null
                             ? nameof(request.RenderScript)
                             : (script = request.RenderCode) != null
                                 ? nameof(request.RenderCode)
                                 : (script = request.RenderLisp) != null
                                     ? nameof(request.RenderLisp)
                                     : null) ??
             ((script = request.EvaluateScriptAsync) != null
                 ? nameof(request.EvaluateScriptAsync)
                 : (script = request.EvaluateCodeAsync) != null
                     ? nameof(request.EvaluateCodeAsync)
                     : (script = request.EvaluateLispAsync) != null
                         ? nameof(request.EvaluateLispAsync)
                         : (script = request.RenderScriptAsync) != null
                             ? nameof(request.RenderScriptAsync)
                             : (script = request.RenderCodeAsync) != null
                                 ? nameof(request.RenderCodeAsync)
                                 : (script = request.RenderLispAsync) != null
                                     ? nameof(request.RenderLispAsync)
                                     : null)
                ?? throw new ArgumentNullException(nameof(request.EvaluateCode));
            
            async Task HandleExceptionAsync(Exception e)
            {
                log.Error(e.Message, e);
                base.Response.StatusCode = 500;
                base.Response.StatusDescription = e.GetType().Name;
                base.Response.ContentType = MimeTypes.PlainText;
                await base.Response.OutputStream.WriteAsync(MemoryProvider.Instance.ToUtf8(e.ToString().AsSpan()));
                await base.Response.EndRequestAsync(skipHeaders:true);
            }

            async Task SetResult(object value, string resultType=" result")
            {
                base.Response.ContentType = MimeTypes.Json;
                base.Response.StatusCode = 200;
                base.Response.StatusDescription = method + resultType;
                await base.Response.EndRequestAsync(skipHeaders:false, async res => {
                    using var ms = MemoryStreamFactory.GetStream();
                    JsonSerializer.SerializeToStream(value, ms);
                    ms.Position = 0;
                    await ms.CopyToAsync(base.Response.OutputStream);
                });
            }
            
            async Task SetOutput(PageResult result)
            {
                base.Response.StatusCode = 200;
                base.Response.StatusDescription = method + " result";
                base.Response.ContentType = MimeTypes.PlainText;
                await base.Response.EndRequestAsync(skipHeaders:false, async res => {
                    using var ms = MemoryStreamFactory.GetStream();
                    await result.RenderToStreamAsync(ms);
                    ms.Position = 0;
                    await ms.CopyToAsync(res.OutputStream);    
                });
            }
            var args = new Dictionary<string,object> {
                [ScriptConstants.Request] = base.Request,
            }; 
                
            if (method.EqualsIgnoreCase(nameof(ScriptTemplateUtils.EvaluateScript)))
                await SetResult(await appHost.ScriptContext.EvaluateAsync(script, args));
            else if (method.EqualsIgnoreCase(nameof(ScriptTemplateUtils.RenderScript)))
                await SetOutput(new PageResult(appHost.ScriptContext.SharpScriptPage(script)).AssignArgs(args));
                
            else if (method.EqualsIgnoreCase(nameof(ScriptCodeUtils.EvaluateCode)))
                await SetResult(await appHost.ScriptContext.EvaluateCodeAsync(ScriptCodeUtils.EnsureReturn(script), args));
            else if (method.EqualsIgnoreCase(nameof(ScriptCodeUtils.RenderCode)))
                await SetOutput(new PageResult(appHost.ScriptContext.CodeSharpPage(script)).AssignArgs(args));
                
            else if (method.EqualsIgnoreCase(nameof(ScriptLispUtils.EvaluateLisp)))
                await SetResult(await appHost.ScriptContext.EvaluateLispAsync(ScriptLispUtils.EnsureReturn(script), args));
            else if (method.EqualsIgnoreCase(nameof(ScriptLispUtils.RenderLisp)))
                await SetOutput(new PageResult(appHost.ScriptContext.LispSharpPage(script)).AssignArgs(args));

            if (base.Response.IsClosed)
                return;

            async Task setResultAsync(Task<object> valueTask, string resultType=" result")
            {
                try
                {
                    base.Response.ContentType = MimeTypes.Json;
                    base.Response.StatusCode = 200;
                    base.Response.StatusDescription = method + resultType;
                    await base.Response.EndRequestAsync(skipHeaders:false, async res => {
                        using var ms = MemoryStreamFactory.GetStream();
                        JsonSerializer.SerializeToStream(await valueTask, ms);
                        await ms.CopyToAsync(base.Response.OutputStream);
                    });
                }
                catch (Exception e)
                {
                    await HandleExceptionAsync(e);
                }
            }
            
            async Task setOutputAsync(PageResult result)
            {
                try
                {
                    base.Response.StatusCode = 200;
                    base.Response.StatusDescription = method + " async result";
                    base.Response.ContentType = MimeTypes.PlainText;
                    await base.Response.EndRequestAsync(skipHeaders:false, async res => {
                        using var ms = MemoryStreamFactory.GetStream();
                        await result.RenderToStreamAsync(ms);
                        await ms.CopyToAsync(res.OutputStream);    
                    });
                }
                catch (Exception e)
                {
                    await HandleExceptionAsync(e);
                }
            }

            if (method.EqualsIgnoreCase(nameof(ScriptTemplateUtils.EvaluateScriptAsync)))
                await Task.Run(async () => await setResultAsync(appHost.ScriptContext.EvaluateAsync(script, args), " async result"));
            else if (method.EqualsIgnoreCase(nameof(ScriptTemplateUtils.RenderScriptAsync)))
                await Task.Run(async () => await setOutputAsync(new PageResult(appHost.ScriptContext.SharpScriptPage(script)).AssignArgs(args)));

            else if (method.EqualsIgnoreCase(nameof(ScriptCodeUtils.EvaluateCodeAsync)))
                await Task.Run(async () => await setResultAsync(appHost.ScriptContext.EvaluateCodeAsync(ScriptCodeUtils.EnsureReturn(script), args), " async result"));
            else if (method.EqualsIgnoreCase(nameof(ScriptCodeUtils.RenderCodeAsync)))
                await Task.Run(async () => await setOutputAsync(new PageResult(appHost.ScriptContext.CodeSharpPage(script)).AssignArgs(args)));

            else if (method.EqualsIgnoreCase(nameof(ScriptLispUtils.EvaluateLispAsync)))
                await Task.Run(async () => await setResultAsync(appHost.ScriptContext.EvaluateLispAsync(ScriptLispUtils.EnsureReturn(script), args), " async result"));
            else if (method.EqualsIgnoreCase(nameof(ScriptLispUtils.RenderLispAsync)))
                await Task.Run(async () => await setOutputAsync(new PageResult(appHost.ScriptContext.LispSharpPage(script)).AssignArgs(args)));
            else throw new NotSupportedException($"Unsupported script API '{method}', supported: " +
                "EvaluateScript/Async, EvaluateCode/Async, EvaluateLisp/Async");
        }
    }
}