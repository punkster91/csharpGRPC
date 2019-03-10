using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Logging;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using Helloworld;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace gRPCServer
{
    public class Program
    {
        const string Host = "127.0.0.1";
        const int Port = 50051;

        public static void Main(string[] args)
        {
            var server = new Server
            {
                Services = { Greeter.BindService(new GreeterImpl()).Intercept(new LogInterceptor()) },
                Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
            };

            var healthImpl = new HealthServiceImpl();
            healthImpl.SetStatus(Greeter.Descriptor.FullName, HealthCheckResponse.Types.ServingStatus.Serving);
            server.Services.Add(Health.BindService(healthImpl));
      
            server.Start();

            Console.WriteLine("Server listening on port " + Port);
            Console.WriteLine("Press any key to shutdown server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

        }

        public class LogInterceptor : Interceptor
        {
            public static ILogger Logger = new ConsoleLogger();

            /// <summary>
            /// Logs all request
            /// </summary>
            /// <typeparam name="TRequest"></typeparam>
            /// <typeparam name="TResponse"></typeparam>
            /// <param name="request"></param>
            /// <param name="context"></param>
            /// <param name="continuation"></param>
            /// <returns></returns>
             public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
            {               
                // log incoming requests
                Logger.Info($"{context.Method} called by {context.Peer} with message: {request}");

                try
                {
                    var sw = Stopwatch.StartNew();
                    var response = base.UnaryServerHandler(request, context, continuation);
                    sw.Stop();

                    // log outgoing responses
                    Logger.Info($"{context.Method} done in {sw.ElapsedMilliseconds}ms");
                    return response;
                }
                catch (Exception ex)
                {
                    // server needs to keep track of errors
                    Logger.Error(ex, $"{context.Method} exception");

                    // return the same error to clients otherwise they only get Status=Unknown and Detail="Exception was thrown by handler" which is not helpful
                    throw;
                }
            }
        }

        public class GreeterImpl : Greeter.GreeterBase
        {
            //public static ILogger Logger = new ConsoleLogger();

            public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
            {
                return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
            }

            public override Task<HelloReply> SayHelloAgain(HelloRequest request, ServerCallContext context)
            {
                 // you can throw RpcException
                 // first error message is available to the client
                 // second idential error message is only available to the server
                var rpcEx = new RpcException(new Status(StatusCode.InvalidArgument, "Specify at least one accounting code"), "Specify at least one accounting code");
                throw rpcEx;

                // ... unhandled exception somewhere later occurs. No worries our log interceptor has a try catch

                // instead of try catch we can set the Status directly and return the response
                context.Status = new Status(StatusCode.InvalidArgument, "Specify at least one accounting code");
                var response = new HelloReply
                {
                    Message = "Hello Again " + request.Name
                };

                return Task.FromResult(response);
            }
            
        }
    }
}
