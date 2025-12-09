using Entities;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContracts;
using Google.Protobuf.WellKnownTypes;
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
                Email = user.Email,
                Password = user.Password
            };

            Console.WriteLine(
                $"UserProto created: Username={userProto.Username}, Password length={userProto.Password?.Length}");

            // Wrap the UserProto in an Any
            var anyPayload = Google.Protobuf.WellKnownTypes.Any.Pack(userProto);

            Console.WriteLine($"Any payload created, type URL: {anyPayload.TypeUrl}");

            // Create the homogenious request
            var request = new Request
            {
                Handler = HandlerType.HandlerUser, // Use HANDLER_USER enum
                Action = ActionType.ActionCreate, // Use ACTION_CREATE enum
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
                    Password = user.Password,
                    Email = responseProto.Email,
                    Points = responseProto.Points
                };
            }
            else if (response.Status == StatusType.StatusError)
            {
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
            Console.WriteLine(
                $"ERROR: gRPC exception. Status: {ex.StatusCode}, Message: {ex.Message}, Detail: {ex.Status.Detail}");

            if (ex.StatusCode == Grpc.Core.StatusCode.Unimplemented)
            {
                throw new InvalidOperationException(
                    "The Java gRPC server does not have the expected method. Please ensure the server is running and implementing the homogenious service.",
                    ex);
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

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            
            var userProto = new UserProto { Id = id };
            var anyPayload = Any.Pack(userProto);

            var request = new Request
            {
                Handler = HandlerType.HandlerUser,
                Action = ActionType.ActionGet,
                Payload = anyPayload
            };

            Console.WriteLine($"Sending GET request to Java server for user id={id}");
            var response = await _grpcClient.handleRequestAsync(request);

            Console.WriteLine($"Response status: {response.Status}");

            if (response.Status == StatusType.StatusOk)
            {
                var responseProto = response.Payload.Unpack<UserProto>();
                Console.WriteLine($"User found: Id={responseProto.Id}, Username={responseProto.Username}, Points={responseProto.Points}");

                return new User
                {
                    Id = responseProto.Id,
                    Username = responseProto.Username,
                    Password = "",
                    Email = responseProto.Email,
                    Points = responseProto.Points
                };
            }

            if (response.Status == StatusType.StatusError)
            {
                Console.WriteLine($"ERROR: Server returned error for user id={id}");
                return null;
            }

            Console.WriteLine($"User not found with id={id}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in GetByIdAsync for id={id}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return null;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            Console.WriteLine($"Updating user: {user.Id} ({user.Username}), points={user.Points}");

            var userProto = new UserProto
            {
                Id = user.Id,
                Username = user.Username,
                Password = user.Password,
                Email = user.Email,
                Points = user.Points // goes to javadb server
            };

            var anyPayload = Any.Pack(userProto);

            var request = new Request
            {
                Handler = HandlerType.HandlerUser,
                Action = ActionType.ActionUpdate,
                Payload = anyPayload
            };

            var response = await _grpcClient.handleRequestAsync(request);

            Console.WriteLine($"UpdateAsync response status: {response.Status}");

            if (response.Status == StatusType.StatusOk)
            {
                var responseProto = response.Payload.Unpack<UserProto>();

                return new User
                {
                    Id = responseProto.Id,
                    Username = responseProto.Username,
                    Password = user.Password, //not sure
                    Email = responseProto.Email,
                    Points = responseProto.Points
                };
            }

            if (response.Status == StatusType.StatusError)
            {
                Console.WriteLine("UpdateAsync: STATUS_ERROR érkezett.");
                throw new InvalidOperationException("Server returned error during update.");
            }

            throw new InvalidOperationException($"Server returned unexpected status: {response.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in UpdateAsync: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            var userProto = new UserProto { Email = email };
            var anyPayload = Any.Pack(userProto);
            
            var request = new Request
            {
                Handler = HandlerType.HandlerUser,
                Action = ActionType.ActionGet,
                Payload = anyPayload
            };

            Console.WriteLine($"Looking up user by email: {email}");
            var response = await _grpcClient.handleRequestAsync(request);
            
            if (response?.Payload != null)
            {
                var responseProto = response.Payload.Unpack<UserProto>();
                
                if (responseProto.Id > 0 && !string.IsNullOrEmpty(responseProto.Email))
                {
                    Console.WriteLine($"User found: {responseProto.Email} (ID: {responseProto.Id})");
                    
                    return new User
                    {
                        Id = responseProto.Id,
                        Username = responseProto.Username,
                        Email = responseProto.Email,
                        Password = responseProto.Password // Hashed password from database
                    };
                }
            }
            
            Console.WriteLine($"User not found with email: {email}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR getting user by email: {ex.Message}");
            return null;
        }
    }
    


    public async Task<User?> GetSingleAsync(int id)
    {
        try
        {
            var userProto = new UserProto { Id = id };
            var anyPayload = Any.Pack(userProto);
            var request = new Request
            {
                Handler = HandlerType.HandlerUser,
                Action = ActionType.ActionGet,
                Payload = anyPayload
            };
            Console.Write($"Getting user by id: {id}");
            var response = await _grpcClient.handleRequestAsync(request);
            if (response.Status == StatusType.StatusOk)
            {
                var responseProto = response.Payload.Unpack<UserProto>();
                if (responseProto.Id>0)
                {
                    return new User
                    {
                        Id = responseProto.Id,
                        Username = responseProto.Username,
                        Email=responseProto.Email,
                        Password = responseProto.Password
                    };
                }
                
            }
            return null;
       

        }
        catch(Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to get user by ID: {ex.Message}");
            return null;
        }
    }
  
  
}