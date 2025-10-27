using Entities;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContracts;
using Sep3_Proto;


namespace Repositories;

public class UserRepository : IUserRepository
{
    private readonly homogeniousService.homogeniousServiceClient _grpcClient;
    
    public UserRepository(homogeniousService.homogeniousServiceClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task<User> AddAsync(User user)
    {   
        try
        {
            Console.WriteLine($"Creating user: {user.Username}");
            
            // Create a UserProto to send as payload
            var userProto = new UserProto
            {
                Username = user.Username,
                Password = user.Password
            };

            Console.WriteLine($"UserProto created: Username={userProto.Username}, Password length={userProto.Password?.Length}");

            // Wrap the UserProto in an Any
            var anyPayload = Google.Protobuf.WellKnownTypes.Any.Pack(userProto);
            
            Console.WriteLine($"Any payload created, type URL: {anyPayload.TypeUrl}");
            
            // Create the homogenious request
            var request = new Request
            {
                Handler = HandlerType.HandlerUser,  // Use HANDLER_USER enum
                Action = ActionType.ActionCreate,   // Use ACTION_CREATE enum
                Payload = anyPayload
            };

            Console.WriteLine($"Sending request: Handler={request.Handler}, Action={request.Action}");
            
            var response = await _grpcClient.handleRequestAsync(request);
            
            Console.WriteLine($"Received response with status: {response.Status}");
            
            if (response.Status == StatusType.StatusOk)
            {
                Console.WriteLine($"Response payload type URL: {response.Payload.TypeUrl}");
                
                // Unpack the response payload (Java server wraps it in Any)
                var responseProto = response.Payload.Unpack<UserProto>();
                Console.WriteLine($"Created user on Java server: {responseProto.Username} with id {responseProto.Id}");

                return new User
                {
                    Id = responseProto.Id,
                    Username = responseProto.Username,
                    Password = user.Password
                };
            }
            else if (response.Status == StatusType.StatusError)
            {
                // Try to unpack error message if it's a string
                try
                {
                    var errorMsg = response.Payload.Unpack<Google.Protobuf.WellKnownTypes.StringValue>();
                    throw new InvalidOperationException($"Server returned error: {errorMsg.Value}");
                }
                catch
                {
                    throw new InvalidOperationException($"Server returned error status: {response.Status}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Server returned status: {response.Status}");
            }
        }
        catch (Grpc.Core.RpcException ex)
        {
            Console.WriteLine($"ERROR: gRPC exception. Status: {ex.StatusCode}, Message: {ex.Message}, Detail: {ex.Status.Detail}");
            
            if (ex.StatusCode == Grpc.Core.StatusCode.Unimplemented)
            {
                throw new InvalidOperationException("The Java gRPC server does not have the expected method. Please ensure the server is running and implementing the homogenious service.", ex);
            }
            
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to create user: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}