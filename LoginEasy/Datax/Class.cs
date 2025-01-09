
public class Log
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Endpoint { get; set; }
    public string Parameters { get; set; }
    public DateTime DateConsultation { get; set; }
}

public class User
{
    public int Id { get; set; }   
    public string Name { get; set; }    
    public string SurName { get; set; }    
    public string Username { get; set; }    
    public string Password { get; set; }    
    public string Email { get; set; }
    public string AuthToken { get; set; }
}

public class Request
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; }  // Type of request
    public string Status { get; set; }  // Status of the request (eg, "Pending", "Approved", "Rejected")
    public DateTime DateConsultation { get; set; }
    public DateTime? DateAnswer { get; set; }
    public string Answer { get; set; } // Response or note on the request
}

public class LoginData
{    
    public string Username { get; set; }    
    public string Password { get; set; }
}

public class ServerResult<T>
{
    public bool Success { get; set; }      // Indicates if the operation was successful
    public string Message { get; set; }     // Custom message
    public T Data { get; set; }             // Result data, e.g., the token or user info
    public string Error { get; set; }       // Error message if any (optional)

    public ServerResult(bool success, string message, T data = default, string error = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Error = error;
    }
}