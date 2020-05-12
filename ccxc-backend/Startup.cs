using Ccxc.Core.HttpServer.Middlewares;
using System.Threading;

namespace ccxc_backend
{
    internal class Startup
    {
        public AutoResetEvent StopSignal { get; set; } = new AutoResetEvent(false);
        public bool Running { get; private set; } = false;

        private Ccxc.Core.HttpServer.Server server;

        internal void Run()
        {
            Running = true;

            //初始化数据库
            Ccxc.Core.Utils.Logger.Info("正在初始化数据库。");

            var dm = new DataServices.DbMaintenance(Config.Config.Options.DbConnStr);
            dm.InitDatabase();

            //注册HTTP控制器组件
            Controllers.ControllerRegister.Regist();

            //启动HTTP服务
            Ccxc.Core.Utils.Logger.Info("正在启动HTTP服务。");
            server = Ccxc.Core.HttpServer.Server.GetServer(Config.Config.Options.HttpPort, "api/v1");
            server.DebugMode = Config.Config.Options.DebugMode;
            server.UseCors().Run();
        }

        internal void Wait()
        {
            StopSignal.WaitOne();
            while (Running)
            {
                StopSignal.WaitOne();
            }
        }

        internal async void StopServer()
        {
            Running = false;
            await server.Stop();
            StopSignal.Set();
        }
    }
}
