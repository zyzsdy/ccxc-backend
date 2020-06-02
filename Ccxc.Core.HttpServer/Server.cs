using Ccxc.Core.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Ccxc.Core.HttpServer
{
    /// <summary>
    /// 一个提供HTTP服务的Kestrel服务器
    /// </summary>
    public class Server
    {

        private int listenPort;
        private string prefix;
        private static object _lockHandlers = new object();
        private static List<HttpHandler> handlerList = new List<HttpHandler>();
        private static ConcurrentDictionary<string, HttpHandler> handlerCache = new ConcurrentDictionary<string, HttpHandler>(); //匹配缓存（由于执行路径匹配很耗时，建立此缓存以避免热点页面经常执行路径匹配）
        private static List<Func<HttpContext, Func<Task>, Task>> middlewares = new List<Func<HttpContext, Func<Task>, Task>>(); //中间件

        /// <summary>
        /// 一个提供HTTP服务的Kestrel服务器
        /// </summary>
        /// <param name="port">监听的端口</param>
        /// <param name="prefix">服务提供的位置，如传入“mims”会提供在http://ip:port/mims下</param>
        public Server(int port, string prefix = "")
        {
            this.listenPort = port;
            this.prefix = prefix;
        }

        /// <summary>
        /// 获取HTTP服务器实例
        /// </summary>
        /// <param name="port">监听的端口</param>
        /// <param name="prefix">服务提供的位置，如传入“mims”会提供在http://ip:port/mims下</param>
        /// <returns></returns>
        public static Server GetServer(int port, string prefix = "")
        {
            return new Server(port, prefix);
        }

        /// <summary>
        /// 运行中的WebHost。
        /// </summary>
        public IWebHost Host { get; private set; }

        /// <summary>
        /// 如果此值设置为True，则出现服务器异常时会向客户端返回错误详情。否则只返回简要的错误信息。默认为True。
        /// </summary>
        public bool DebugMode { get; set; } =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>
        /// 启动HTTP服务器。
        /// 请确保所有中间件都已经注入，最后再调用Run方法启动服务器。
        /// </summary>
        /// <returns></returns>
        public Task Run()
        {
            Host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, listenPort);
                })
                .Configure(app =>
                {
                    //增加路径前缀！警告！该中间件只能位于第一个！任何其他中间件都必须加入在这段代码之后。
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        app.UsePathBase(new PathString("/" + prefix));
                    }

                    //日志中间件
                    app.Use(async (ctx, next) =>
                    {
                        //记录真实IP
                        var realIp = ctx.Request.Headers["CF-Connecting-IP"];
                        var forwardIp = ctx.Request.Headers["X-Forwarded-For"];
                        var ua = ctx.Request.Headers["User-Agent"];

                        ctx.Items.Add("RealIp", realIp);
                        ctx.Items.Add("ForwardIp", forwardIp);
                        ctx.Items.Add("UserAgent", ua);

                        //输出Log
                        Logger.Info($"HTTP Request: {ctx.Request.Method} {ctx.Request.PathBase} {ctx.Request.Path} {ctx.Request.QueryString} {realIp} {forwardIp}");
                        await next.Invoke();
                    });

                    //错误处理中间件
                    if (DebugMode)
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    else
                    {
                        app.UseExceptionHandler(new ExceptionHandlerOptions
                        {
                            ExceptionHandler = ctx =>
                            {
                                var exf = ctx.Features.Get<IExceptionHandlerFeature>();
                                var ex = exf?.Error;
                                if (ex != null)
                                {
                                    Logger.ErrorAsync(ex.ToString());
                                }

                                if (ctx.Response.Headers.Any(h => h.Key == "Access-Control-Allow-Origin"))
                                {
                                    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
                                }
                                else
                                {
                                    ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                                }
                                return Response.SimpleResponse(ctx.Response, 500, "服务器内部错误：" + ex.Message);
                            }
                        });
                    }

                    //向app添加所有注册的中间件
                    foreach (var middleware in middlewares)
                    {
                        app.Use(middleware);
                    }

                    //最终控制器
                    app.Run(http =>
                    {
                        return Task.Run(() => {
                            var request = http.Request;
                            var response = http.Response;
                            var method = http.Request.Method;
                            var path = http.Request.Path;
                            var pathbase = http.Request.PathBase;
                            var remoteIp = http.Connection?.RemoteIpAddress?.ToString();
                            dynamic pathParam = new ExpandoObject();

                            if (!pathbase.StartsWithSegments("/" + prefix))
                            {
                                return Response.SimpleResponse(response, 404, "Not found.");
                            }

                            //寻找控制器缓存
                            var cacheKey = $"R$$///{method}$===${path}";
                            handlerCache.TryGetValue(cacheKey, out HttpHandler handler);
                            if (handler == null)
                            {
                                //无缓存，从List中寻找第一个匹配。
                                handler = handlerList.FirstOrDefault(h => h.Method == method && Router.Match(path, h.Path, out pathParam));
                                if (handler != null)
                                {
                                    handlerCache.TryAdd(cacheKey, handler);
                                }
                            }
                            else
                            {
                                Router.Match(path, handler.Path, out pathParam);
                            }

                            if (handler != null)
                            {
                                try
                                {
                                    var req = new Request(request, pathParam)
                                    {
                                        ContextItems = http.Items
                                    };
                                    var res = new Response(response);

                                    return handler.Handler.Invoke(req, res);
                                }
                                catch (Exception e)
                                {
                                    Logger.Error(e.ToString());
                                    if (DebugMode)
                                    {
                                        return Response.SimpleResponse(response, 500, "Internal Server Error: " + e.ToString());
                                    }
                                    else
                                    {
                                        return Response.SimpleResponse(response, 500, "Internal Server Error.");
                                    }
                                }
                            }

                            return Response.SimpleResponse(response, 404, "We try our best but not found.");
                        });
                    });
                })
                .Build();

            var url = $"http://0.0.0.0:{listenPort}";
            Logger.Info($"HTTP Server listening on {url}/{prefix}");

            return Host.StartAsync();
        }

        /// <summary>
        /// 停止服务器运行
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await Host?.StopAsync();
        }

        /// <summary>
        /// 将一个包含多个HTTP处理方法的Controller类注册到HTTP服务器上
        /// </summary>
        /// <typeparam name="T">继承HttpController的控制器</typeparam>
        public static void RegisterController<T>() where T : HttpController, new()
        {
            RegisterController(typeof(T));
        }

        /// <summary>
        /// 将一个包含多个HTTP处理方法的Controller类注册到HTTP服务器上
        /// </summary>
        /// <param name="controller">控制器类的对象</param>
        public static void RegisterController(object controller)
        {
            RegisterController(controller.GetType());
        }

        /// <summary>
        /// 将一个包含多个HTTP处理方法的Controller类注册到HTTP服务器上
        /// </summary>
        /// <param name="TYPE">控制器类的类型</param>
        public static void RegisterController(Type TYPE)
        {
            if (!TYPE.IsSubclassOf(typeof(HttpController)))
            {
                throw new HttpHandlerRegisterException("HTTP Handler 控制器类必须继承 HttpController");
            }
            var handlerMethods = TYPE.GetMethods().Where(method => method.GetCustomAttributes().Any(attr => attr is HttpHandlerAttribute));
            foreach (var handler in handlerMethods)
            {
                var paraInfos = handler.GetParameters();
                if (paraInfos.Length != 2)
                {
                    throw new HttpHandlerRegisterException("HTTP Handler 控制器方法必须含有两个参数 Request, Response");
                }
                else
                {
                    if (paraInfos[0].ParameterType != typeof(Request) || paraInfos[1].ParameterType != typeof(Response))
                    {
                        throw new HttpHandlerRegisterException("HTTP Handler 控制器方法必须含有两个参数 Request, Response");
                    }
                }

                if (handler.ReturnType != typeof(Task))
                {
                    throw new HttpHandlerRegisterException("HTTP Handler 必须返回 Task");
                }


                foreach (var attr in handler.GetCustomAttributes().Where(attr => attr is HttpHandlerAttribute))
                {
                    var httpAttr = attr as HttpHandlerAttribute;
                    RegisterHandler(new HttpHandler
                    {
                        Method = httpAttr.Method,
                        Path = httpAttr.Path,
                        Handler = (request, response) =>
                        {
                            object[] parameters = new object[] { request, response };
                            var controller = Activator.CreateInstance(TYPE);
                            var result = handler.Invoke(controller, parameters);
                            return result as Task;
                        }
                    });
                    break;
                }
            }
        }

        /// <summary>
        /// 将HTTP Handler注册到服务器上。
        /// </summary>
        /// <param name="handlers">HTTP控制器列表</param>
        public static void RegisterHandler(IEnumerable<HttpHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                RegisterHandler(handler);
            }
        }

        /// <summary>
        /// 将HTTP Handler注册到服务器上。
        /// </summary>
        /// <param name="handler">HTTP控制器</param>
        /// <returns></returns>
        public static void RegisterHandler(HttpHandler handler)
        {
            if (handlerList.Any(h => h.Path == handler.Path && h.Method == handler.Method))
            {
                throw new HttpHandlerRegisterException($"{handler.Method} {handler.Path} 无法重复被绑定。");
            }
            else
            {
                lock (_lockHandlers)
                {
                    handlerList.Add(handler);
                }
            }
        }

        /// <summary>
        /// 将HTTP Handler注册到服务器上。
        /// </summary>
        /// <param name="handler">HTTP控制器</param>
        /// <returns></returns>
        public Server Register(HttpHandler handler)
        {
            RegisterHandler(handler);
            return this;
        }

        /// <summary>
        /// 将HTTP Handler注册到服务器上
        /// </summary>
        /// <param name="method">方法名，如GET、POST等</param>
        /// <param name="path">匹配的路径，以冒号开头的值会匹配任意内容，并将匹配结果填入Request.Params</param>
        /// <param name="handler">执行的方法，可以为一个async (request, response) => { } 。请一定在其中调用await或使用await Task.Run</param>
        /// <returns></returns>
        public static void RegisterHandler(string method, string path, Func<Request, Response, Task> handler)
        {
            var tempHandler = new HttpHandler
            {
                Method = method,
                Path = path,
                Handler = handler
            };
            RegisterHandler(tempHandler);
        }

        /// <summary>
        /// 将HTTP Handler注册到服务器上
        /// </summary>
        /// <param name="method">方法名，如GET、POST等</param>
        /// <param name="path">匹配的路径，以冒号开头的值会匹配任意内容，并将匹配结果填入Request.Params</param>
        /// <param name="handler">执行的方法，可以为一个async (request, response) => { } 。请一定在其中调用await或使用await Task.Run</param>
        /// <returns></returns>
        public Server Register(string method, string path, Func<Request, Response, Task> handler)
        {
            RegisterHandler(method, path, handler);
            return this;
        }

        public static void AddMiddleware(Func<HttpContext, Func<Task>, Task> middleware)
        {
            middlewares.Add(middleware);
        }

        public Server Use(Func<HttpContext, Func<Task>, Task> middleware)
        {
            AddMiddleware(middleware);
            return this;
        }

        public static List<string> GetInterfaces()
        {
            return handlerList.Select(it => $"{it.Method} {it.Path}").ToList();
        }
    }
}
