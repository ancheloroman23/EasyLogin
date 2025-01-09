using EasyLogin.Datax;
using EasyLogin.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Pleases, Into the JWT token with Bearer [Token]",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"}
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(option =>
    option.UseSqlServer("name=DefaultConnection"));

// Register filter personalized
builder.Services.AddScoped<ValidateTokenAttribute>();

builder.Services.AddSingleton<PasswordHasherService>();

#region Add Authentication and Authorization

// Add Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

#endregion


builder.Services.AddAuthentication();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region Register User EndPoint

//Register User EndPoint
app.MapPost("/registerUser", async (ApplicationDbContext db, PasswordHasherService hasher, [FromBody] User newUser) =>
{
    // Validar si el usuario ya existe
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == newUser.Username || u.Email == newUser.Email);
    if (existingUser != null)
    {
        return Results.Ok(new ServerResult<string>(false, "Registration failed", error: "Username or email already in use"));
    }

    // Validar campos requeridos
    if (string.IsNullOrWhiteSpace(newUser.Username) || string.IsNullOrWhiteSpace(newUser.Password) || string.IsNullOrWhiteSpace(newUser.Email))
    {
        return Results.Ok(new ServerResult<string>(false, "Error", error: "All fields are required"));
    }

    // Crear usuario con contraseña encriptada
    var user = new User
    {

        Name = newUser.Name,
        SurName = newUser.SurName,
        Username = newUser.Username,
        Password = hasher.HashPassword(newUser.Password), // Encriptar la contraseña
        Email = newUser.Email,
        AuthToken = Guid.NewGuid().ToString()
    };

    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();

    user.Password = "🤣😁👍";
    user.Id = 0;

    return Results.Ok(new ServerResult<User>(true, "Registration successful", user));
});

#endregion

#region Login Endpoint

// Login Endpoint
app.MapPost("/login", async (ApplicationDbContext db, PasswordHasherService hasher, [FromBody] LoginData user) =>
{
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username);

    if (existingUser == null || !hasher.VerifyPassword(existingUser.Password, user.Password))
    {
        return Results.Ok(new ServerResult<string>(false, "Login failed", error: "Username or password incorrect"));
    }

    await LogRegister(db, existingUser, "/login", $"username={user.Username}");

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new Claim[]
        {
            new Claim("id", existingUser.Id.ToString())
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    // Guardar el token en la base de datos
    existingUser.AuthToken = tokenString;
    await db.SaveChangesAsync();

    existingUser.Password = "🤣😁👍";
    existingUser.Id = 0;

    return Results.Ok(new ServerResult<User>(true, "Login successful", existingUser));
});

#endregion

#region User information Endpoint

//User information
app.MapGet("/user_info", async (ApplicationDbContext db, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    var user = await db.Users.FirstOrDefaultAsync(u => u.AuthToken == token);
    if (user == null || user.AuthToken != token)
    {
        return Results.Ok(new ServerResult<string>(false, "Unauthorized", error: "Invalid token"));
    }
    user.Password = "🤣😁👍";
    user.Id = 0;
    return Results.Ok(new ServerResult<User>(true, "User login", user));
});

#endregion

#region Password change Endpoint

// Password change Endpoint
app.MapPost("/change_password", async (ApplicationDbContext db, PasswordHasherService hasher, HttpContext context, [FromBody] PasswordResetData data) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    var user = await db.Users.FirstOrDefaultAsync(u => u.AuthToken == token);

    if (user == null || user.AuthToken != token)
    {
        return Results.Ok(new ServerResult<string>(false, "Unauthorized", error: "Invalid token"));
    }

    if (!hasher.VerifyPassword(user.Password, data.OldPassword))
    {
        return Results.Ok(new ServerResult<string>(false, "Error", error: "The current password is incorrect"));
    }

    if (data.NewPassword != data.ConfirmPassword)
    {
        return Results.Ok(new ServerResult<string>(false, "Error", error: "Passwords don't match"));
    }

    user.Password = hasher.HashPassword(data.NewPassword);
    await db.SaveChangesAsync();

    return Results.Ok(new ServerResult<string>(true, "Password changed successfully"));
});

#endregion


app.MapControllers();

app.Run();

#region Additional Configuration

async Task LogRegister(ApplicationDbContext db, User user, string endpoint, string parameters)
{
var log = new Log
{
UserId = user.Id,
Endpoint = endpoint,
Parameters = parameters,
DateConsultation = DateTime.Now
};
db.Logs.Add(log);
await db.SaveChangesAsync();
}

public class PasswordResetData
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }

    public string ConfirmPassword { get; set; }
}

#endregion