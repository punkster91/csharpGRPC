using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Logging;
using Helloworld;
using System;
using static Grpc.Health.V1.Health;

namespace gRPCClient
{
    public class Program
    {
        const string Host = "127.0.0.1";
        const int Port = 50051;
        //public static ILogger Logger = new ConsoleLogger();

        public static void Main(string[] args)
        {

            var channel = new Channel($"{Host}:{Port}", ChannelCredentials.Insecure);
            var interceptedChannel = channel.Intercept(new LogInterceptor());
            var client = new Greeter.GreeterClient(interceptedChannel);

            Console.WriteLine($"Client connecting to server at {Host}:{Port}");

            var healthClient = new HealthClient(channel);
            var reply3 = healthClient.Check(new Grpc.Health.V1.HealthCheckRequest() { Service = "helloworld.Greeter" });

            var user = "brandon chen";
            var request = new HelloRequest { Name = user };


            var reply = client.SayHello(request);
            // SayHelloAgain throws an error. reply2 will be null
            var reply2 = client.SayHelloAgain(request);

            Console.WriteLine("Press any key to continue after exception...");
            Console.ReadKey();

            // demonstrate that the server is still up when SayHelloAgain executed
            var reply4 = client.SayHello(request);

            Console.WriteLine("Press any key to shutdown channel...");
            Console.ReadKey();

            channel.ShutdownAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Logs all requests
        /// </summary>
        public class LogInterceptor : Interceptor
        {
            public static ILogger Logger = new ConsoleLogger();

            public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
            {
                // log outgoing requests
                Logger.Info($"{context.Method.FullName} request: {request}");

                // try catch on this for every request, what are performance ramifications?
                try
                {
                    var response = base.BlockingUnaryCall(request, context, continuation);
                    Logger.Info($"{context.Method.FullName} response: {response}");
                    return response;
                }
                catch (RpcException ex)
                {
                    Logger.Error(ex, $"{context.Method.FullName} exception. StatusCode: {ex.Status.StatusCode}. Detail={ex.Status.Detail}");                   
                }
                return null;
            }
        }
    }
}
